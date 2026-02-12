import { useEffect, useState } from 'react'
import { useSearchParams, Link } from 'react-router-dom'
import { indexingApi } from '../../modules/indexing'
import { useContextStore } from '../../modules/core/store/context-store'
import { LoadingSpinner, ErrorBanner } from '../../shared/components'
import type { SearchResult } from '../../modules/indexing'

const styles: Record<string, React.CSSProperties> = {
  container: { maxWidth: 900, color: 'var(--page-text)' },
  title: { marginTop: 0, marginBottom: '1rem', color: 'var(--page-text)' },
  searchBox: { marginBottom: '1.5rem' },
  input: {
    width: '100%',
    maxWidth: 500,
    padding: '0.75rem',
    fontSize: '1rem',
    borderRadius: 6,
    border: '1px solid var(--input-border)',
    background: 'var(--input-bg)',
    color: 'var(--input-text)',
  },
  results: { marginTop: '1.5rem' },
  resultItem: {
    padding: '1rem',
    marginBottom: '0.75rem',
    border: '1px solid var(--page-border)',
    borderRadius: 6,
    background: 'var(--page-bg-elevated)',
    color: 'var(--page-text)',
  },
  resultTitle: { margin: '0 0 0.5rem 0', fontSize: '1.125rem', color: 'var(--page-text)' },
  resultMeta: { fontSize: '0.875rem', color: 'var(--page-text-muted)', marginBottom: '0.25rem' },
  link: { color: '#0d6efd', textDecoration: 'none' },
  pagination: { marginTop: '1.5rem', display: 'flex', gap: '0.5rem', alignItems: 'center' },
  button: { padding: '0.4rem 0.75rem', fontSize: '0.875rem', cursor: 'pointer', borderRadius: 4, border: '1px solid var(--input-border)', background: 'var(--input-bg)', color: 'var(--page-text)' },
  hint: { color: 'var(--page-text-muted)', fontSize: '0.875rem' },
  error: { color: '#dc3545', marginBottom: '0.5rem' },
  empty: { color: 'var(--page-text-muted)', fontSize: '0.875rem', padding: '2rem', textAlign: 'center' },
}

const PAGE_SIZE = 20

export function SearchPage() {
  const { currentProjectId } = useContextStore()
  const [searchParams, setSearchParams] = useSearchParams()
  const queryParam = searchParams.get('q') ?? ''
  const pageParam = parseInt(searchParams.get('page') ?? '1', 10)

  const [query, setQuery] = useState(queryParam)
  const [page, setPage] = useState(pageParam)
  const [result, setResult] = useState<SearchResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!query.trim()) {
      setResult(null)
      return
    }
    setLoading(true)
    setError(null)
    indexingApi
      .search({ q: query.trim(), page, pageSize: PAGE_SIZE })
      .then((data) => setResult(data ?? { total: 0, items: [] }))
      .catch((e) => setError(e instanceof Error ? e.message : 'Error al buscar'))
      .finally(() => setLoading(false))
  }, [query, page])

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    setPage(1)
    setSearchParams({ q: query.trim(), page: '1' })
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.total / PAGE_SIZE)) : 0

  return (
    <div style={styles.container}>
      <h1 style={styles.title}>Búsqueda</h1>
      <p style={styles.hint}>
        Busca contenido publicado. Los resultados provienen de la Indexing API (Elasticsearch).
      </p>

      <form onSubmit={handleSearch} style={styles.searchBox}>
        <input
          type="text"
          style={styles.input}
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Buscar contenido publicado..."
        />
        <button type="submit" style={{ ...styles.button, marginTop: '0.5rem' }}>
          Buscar
        </button>
      </form>

      {error && <ErrorBanner message={error} />}
      {loading && <LoadingSpinner text="Buscando…" />}

      {result && !loading && (
        <>
          <p style={styles.hint}>
            {result.total === 0
              ? 'No se encontraron resultados.'
              : `Se encontraron ${result.total} resultado${result.total !== 1 ? 's' : ''}.`}
          </p>

          {result.items.length > 0 && (
            <div style={styles.results}>
              {result.items.map((item) => (
                <div key={`${item.contentId}-${item.versionId}`} style={styles.resultItem}>
                  <h3 style={styles.resultTitle}>
                    <Link
                      to={currentProjectId ? `/content/${item.contentId}/edit` : '#'}
                      style={styles.link}
                    >
                      {item.title}
                    </Link>
                  </h3>
                  <div style={styles.resultMeta}>
                    Tipo: {item.contentType} · Publicado: {item.publishedAt ? new Date(item.publishedAt).toLocaleDateString() : '—'}
                  </div>
                </div>
              ))}
            </div>
          )}

          {totalPages > 1 && (
            <div style={styles.pagination}>
              <button
                type="button"
                style={styles.button}
                disabled={page <= 1}
                onClick={() => {
                  const newPage = page - 1
                  setPage(newPage)
                  setSearchParams({ q: query.trim(), page: String(newPage) })
                }}
              >
                Anterior
              </button>
              <span style={styles.hint}>
                Página {page} de {totalPages} ({result.total} en total)
              </span>
              <button
                type="button"
                style={styles.button}
                disabled={page >= totalPages}
                onClick={() => {
                  const newPage = page + 1
                  setPage(newPage)
                  setSearchParams({ q: query.trim(), page: String(newPage) })
                }}
              >
                Siguiente
              </button>
            </div>
          )}
        </>
      )}

      {!query.trim() && !loading && (
        <div style={styles.empty}>
          Introduce un término de búsqueda y presiona "Buscar" para encontrar contenido publicado.
        </div>
      )}
    </div>
  )
}
