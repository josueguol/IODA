import { useEffect, useState } from 'react'
import { useSearchParams, Link } from 'react-router-dom'
import { indexingApi } from '../../modules/indexing'
import { useContextStore } from '../../modules/core/store/context-store'
import { LoadingSpinner, ErrorBanner } from '../../shared/components'
import type { SearchResult } from '../../modules/indexing'
import './SearchPage.css'

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
    <div className="search-page">
      <h1 className="search-page__title">Búsqueda</h1>
      <p className="search-page__hint">
        Busca contenido publicado. Los resultados provienen de la Indexing API (Elasticsearch).
      </p>

      <form onSubmit={handleSearch} className="search-page__search-box">
        <input
          type="text"
          className="search-page__input"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Buscar contenido publicado..."
        />
        <button type="submit" className="search-page__button search-page__submit">
          Buscar
        </button>
      </form>

      {error && <ErrorBanner message={error} />}
      {loading && <LoadingSpinner text="Buscando…" />}

      {result && !loading && (
        <>
          <p className="search-page__hint">
            {result.total === 0
              ? 'No se encontraron resultados.'
              : `Se encontraron ${result.total} resultado${result.total !== 1 ? 's' : ''}.`}
          </p>

          {result.items.length > 0 && (
            <div className="search-page__results">
              {result.items.map((item) => (
                <div key={`${item.contentId}-${item.versionId}`} className="search-page__result-item">
                  <h3 className="search-page__result-title">
                    <Link
                      to={currentProjectId ? `/content/editor?contentId=${item.contentId}` : '#'}
                      className="search-page__link"
                    >
                      {item.title}
                    </Link>
                  </h3>
                  <div className="search-page__result-meta">
                    Tipo: {item.contentType} · Publicado: {item.publishedAt ? new Date(item.publishedAt).toLocaleDateString() : '—'}
                  </div>
                </div>
              ))}
            </div>
          )}

          {totalPages > 1 && (
            <div className="search-page__pagination">
              <button
                type="button"
                className="search-page__button"
                disabled={page <= 1}
                onClick={() => {
                  const newPage = page - 1
                  setPage(newPage)
                  setSearchParams({ q: query.trim(), page: String(newPage) })
                }}
              >
                Anterior
              </button>
              <span className="search-page__hint">
                Página {page} de {totalPages} ({result.total} en total)
              </span>
              <button
                type="button"
                className="search-page__button"
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
        <div className="search-page__empty">
          Introduce un término de búsqueda y presiona "Buscar" para encontrar contenido publicado.
        </div>
      )}
    </div>
  )
}
