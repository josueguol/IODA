import { chromium } from 'playwright'

const FRONTEND_URL = 'http://127.0.0.1:5173'
const CORE_API = 'http://localhost:5001'
const IDENTITY_API = 'http://localhost:5002'
const EMAIL = process.env.SMOKE_EMAIL ?? 'josue.guol@gmail.com'
const PASSWORD = process.env.SMOKE_PASSWORD ?? 'q1w2e3r4'

async function jsonOrThrow(res, label) {
  if (!res.ok) {
    const body = await res.text()
    throw new Error(`${label} failed: ${res.status} ${body}`)
  }
  return res.json()
}

async function loginApi() {
  const res = await fetch(`${IDENTITY_API}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: EMAIL, password: PASSWORD }),
  })
  return jsonOrThrow(res, 'login')
}

async function getContextIds(accessToken) {
  const auth = { Authorization: `Bearer ${accessToken}` }

  const projects = await jsonOrThrow(
    await fetch(`${CORE_API}/api/projects`, { headers: auth }),
    'list projects'
  )
  const projectId = projects?.items?.[0]?.id
  if (!projectId) throw new Error('No projectId found')

  const environments = await jsonOrThrow(
    await fetch(`${CORE_API}/api/projects/${projectId}/environments`, { headers: auth }),
    'list environments'
  )
  const environmentId = environments?.[0]?.id
  if (!environmentId) throw new Error('No environmentId found')

  const sites = await jsonOrThrow(
    await fetch(`${CORE_API}/api/projects/${projectId}/sites?environmentId=${environmentId}`, { headers: auth }),
    'list sites'
  )
  const siteId = sites?.[0]?.id
  if (!siteId) throw new Error('No siteId found')

  return { projectId, environmentId, siteId }
}

async function getRichTextContentCandidate(accessToken, projectId, siteId) {
  const auth = { Authorization: `Bearer ${accessToken}` }
  const list = await jsonOrThrow(
    await fetch(`${CORE_API}/api/projects/${projectId}/content?page=1&pageSize=50&siteId=${siteId}`, { headers: auth }),
    'list content'
  )

  const items = list?.items ?? []
  for (const item of items) {
    const detail = await jsonOrThrow(
      await fetch(`${CORE_API}/api/projects/${projectId}/content/${item.id}`, { headers: auth }),
      `get content ${item.id}`
    )
    const fields = detail?.fields ?? {}
    const candidateKey = Object.keys(fields).find((k) => {
      const v = fields[k]
      return typeof v === 'string' || (typeof v === 'object' && v !== null)
    })
    if (candidateKey) {
      return { contentId: item.id, candidateFieldKey: candidateKey }
    }
  }

  throw new Error('No content candidate found for Case 9')
}

async function getContent(accessToken, projectId, contentId) {
  const auth = { Authorization: `Bearer ${accessToken}` }
  return jsonOrThrow(
    await fetch(`${CORE_API}/api/projects/${projectId}/content/${contentId}`, { headers: auth }),
    'get content after save'
  )
}

async function run() {
  const login = await loginApi()
  const token = login.accessToken
  const { projectId, environmentId, siteId } = await getContextIds(token)
  const { contentId, candidateFieldKey } = await getRichTextContentCandidate(token, projectId, siteId)

  const browser = await chromium.launch({ headless: true })
  const context = await browser.newContext()

  await context.addInitScript(({ p, e, s }) => {
    sessionStorage.setItem('ioda_context_project_id', p)
    sessionStorage.setItem('ioda_context_environment_id', e)
    sessionStorage.setItem('ioda_context_site_id', s)
  }, { p: projectId, e: environmentId, s: siteId })

  const page = await context.newPage()

  await page.goto(`${FRONTEND_URL}/login`, { waitUntil: 'domcontentloaded' })
  await page.fill('#login-email', EMAIL)
  await page.fill('#login-password', PASSWORD)
  await Promise.all([
    page.waitForURL('**/', { timeout: 20000 }),
    page.click('button[type="submit"]'),
  ])

  await page.goto(`${FRONTEND_URL}/content/editor?contentId=${contentId}`, { waitUntil: 'networkidle' })
  await page.getByRole('heading', { name: 'Editar contenido' }).waitFor({ timeout: 20000 })

  const insertButton = page.getByRole('button', { name: 'Insertar media' })
  await insertButton.waitFor({ timeout: 15000 })
  await insertButton.click()

  const dialog = page.locator('[role="dialog"]')
  await dialog.getByText('Seleccionar desde Multimedia').waitFor({ timeout: 10000 })

  const mediaCard = dialog.locator('button:has(p)').first()
  await mediaCard.waitFor({ timeout: 10000 })
  await mediaCard.click()
  await dialog.waitFor({ state: 'hidden', timeout: 10000 })

  const saveResponsePromise = page.waitForResponse(
    (res) =>
      res.url().includes(`/api/projects/${projectId}/content/${contentId}`) &&
      res.request().method() === 'PUT',
    { timeout: 20000 }
  )
  await page.getByRole('button', { name: 'Guardar cambios' }).click()
  const saveResponse = await saveResponsePromise
  if (!saveResponse.ok()) {
    throw new Error(`Save failed with status ${saveResponse.status()}`)
  }

  await page.reload({ waitUntil: 'networkidle' })

  const updated = await getContent(token, projectId, contentId)
  const fieldValue = updated?.fields?.[candidateFieldKey]
  const serialized = typeof fieldValue === 'string' ? fieldValue : JSON.stringify(fieldValue ?? null)

  const hasMediaUrl =
    serialized.includes('/api/projects/') &&
    serialized.includes('/media/') &&
    serialized.includes('/file')

  if (!hasMediaUrl) {
    throw new Error('Persistence validation failed: no media URL found in updated field value')
  }

  await browser.close()

  const result = {
    ok: true,
    email: EMAIL,
    roleHint: 'SuperAdmin',
    projectId,
    environmentId,
    siteId,
    contentId,
    fieldKeyValidated: candidateFieldKey,
    saveStatus: saveResponse.status(),
    persistedMediaUrl: true,
    executedAt: new Date().toISOString(),
  }
  console.log(JSON.stringify(result, null, 2))
}

run().catch((err) => {
  console.error(err?.stack ?? String(err))
  process.exit(1)
})
