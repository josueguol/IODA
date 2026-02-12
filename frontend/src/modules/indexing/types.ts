/**
 * Tipos alineados con la Indexing API (SearchResultDto, IndexedContentHitDto).
 */

export interface IndexedContentHit {
  contentId: string
  versionId: string
  title: string
  contentType: string
  publishedAt: string
}

export interface SearchResult {
  total: number
  items: IndexedContentHit[]
}
