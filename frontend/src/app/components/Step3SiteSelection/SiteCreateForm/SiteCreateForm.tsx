import { X } from 'lucide-react'
import { ErrorBanner } from '../../../../shared/components'

export interface SiteCreateFormProps {
  name: string
  domain: string
  subdomain: string
  subpath: string
  themeId: string
  urlTemplate: string
  error: string | null
  saving: boolean
  onNameChange: (value: string) => void
  onDomainChange: (value: string) => void
  onSubdomainChange: (value: string) => void
  onSubpathChange: (value: string) => void
  onThemeIdChange: (value: string) => void
  onUrlTemplateChange: (value: string) => void
  onSubmit: () => void
  onCancel?: () => void
  idPrefix?: string
}

export function SiteCreateForm({
  name,
  domain,
  subdomain,
  subpath,
  themeId,
  urlTemplate,
  error,
  saving,
  onNameChange,
  onDomainChange,
  onSubdomainChange,
  onSubpathChange,
  onThemeIdChange,
  onUrlTemplateChange,
  onSubmit,
  onCancel,
  idPrefix = 'step3',
}: SiteCreateFormProps) {
  const nameId = `${idPrefix}-site-name`
  const domainId = `${idPrefix}-site-domain`
  const subdomainId = `${idPrefix}-site-subdomain`
  const subpathId = `${idPrefix}-site-subpath`
  const themeIdField = `${idPrefix}-site-theme`
  const urlTemplateId = `${idPrefix}-site-url-template`
  const titleId = `${idPrefix}-form-title`

  return (
    <div className="onboarding-step__form-wrap onboarding-step__form-wrap--centered">
      {onCancel && (
        <button
          type="button"
          className="onboarding-step__form-close"
          onClick={onCancel}
          disabled={saving}
          aria-label="Cancelar creación de sitio"
        >
          <X size={20} strokeWidth={2} />
        </button>
      )}
      <h3 className="onboarding-step__form-title" id={titleId}>
        Datos del sitio
      </h3>
      <form
        className="onboarding-step__form onboarding-step__form--wide"
        onSubmit={(e) => {
          e.preventDefault()
          onSubmit()
        }}
        noValidate
        aria-labelledby={titleId}
      >
        <div className="onboarding-step__field">
          <label className="onboarding-step__label" htmlFor={nameId}>
            Nombre del sitio *
          </label>
          <input
            id={nameId}
            type="text"
            className="onboarding-step__input"
            value={name}
            onChange={(e) => onNameChange(e.target.value)}
            placeholder="Ej. Sitio principal"
            disabled={saving}
            required
            aria-invalid={!!error}
          />
        </div>
        <div className="onboarding-step__field">
          <label className="onboarding-step__label" htmlFor={domainId}>
            Dominio *
          </label>
          <input
            id={domainId}
            type="text"
            className="onboarding-step__input"
            value={domain}
            onChange={(e) => onDomainChange(e.target.value)}
            placeholder="example.com"
            disabled={saving}
            required
          />
        </div>
        <div className="onboarding-step__field">
          <label className="onboarding-step__label" htmlFor={subdomainId}>
            Subdominio (opcional)
          </label>
          <input
            id={subdomainId}
            type="text"
            className="onboarding-step__input"
            value={subdomain}
            onChange={(e) => onSubdomainChange(e.target.value)}
            placeholder="www o blog"
            disabled={saving}
          />
        </div>
        <div className="onboarding-step__field">
          <label className="onboarding-step__label" htmlFor={subpathId}>
            Subruta (opcional)
          </label>
          <input
            id={subpathId}
            type="text"
            className="onboarding-step__input"
            value={subpath}
            onChange={(e) => onSubpathChange(e.target.value)}
            placeholder="/blog"
            disabled={saving}
          />
        </div>
        <div className="onboarding-step__field">
          <label className="onboarding-step__label" htmlFor={themeIdField}>
            Tema (opcional)
          </label>
          <input
            id={themeIdField}
            type="text"
            className="onboarding-step__input"
            value={themeId}
            onChange={(e) => onThemeIdChange(e.target.value)}
            placeholder="theme-default"
            disabled={saving}
          />
        </div>
        <div className="onboarding-step__field">
          <label className="onboarding-step__label" htmlFor={urlTemplateId}>
            Plantilla URL (opcional)
          </label>
          <input
            id={urlTemplateId}
            type="text"
            className="onboarding-step__input"
            value={urlTemplate}
            onChange={(e) => onUrlTemplateChange(e.target.value)}
            placeholder="/{slug}"
            disabled={saving}
          />
        </div>
        {error && <ErrorBanner message={error} />}
        <button type="submit" className="onboarding-step__submit" disabled={saving}>
          {saving ? 'Creando…' : 'Crear'}
        </button>
      </form>
    </div>
  )
}
