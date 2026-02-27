import { useState, useMemo } from 'react'
import { coreApi } from '../../../modules/core/api/core-api'
import type { Content, ContentBlock, ContentSchema } from '../../../modules/core/types'
import './ContentBlocksSection.css'

const ALL_BLOCK_TYPES = [
  { value: 'hero', label: 'Hero' },
  { value: 'text', label: 'Texto' },
  { value: 'image', label: 'Imagen' },
] as const

export interface ContentBlocksSectionProps {
  projectId: string
  contentId: string
  content: Content
  /** Schema del contenido; si tiene allowedBlockTypes, solo se ofrecen esos tipos al añadir. */
  schema?: ContentSchema | null
  onContentUpdated: () => void
}

export function ContentBlocksSection({
  projectId,
  contentId,
  content,
  schema,
  onContentUpdated,
}: ContentBlocksSectionProps) {
  const blocks = (content.blocks ?? []).slice().sort((a, b) => a.order - b.order)
  const allowedTypes = useMemo(() => {
    const list = schema?.allowedBlockTypes
    if (!list || list.length === 0) return []
    return ALL_BLOCK_TYPES.filter((t) => list.some((r) => r.blockType.toLowerCase() === t.value.toLowerCase()))
  }, [schema?.allowedBlockTypes])
  const [addType, setAddType] = useState<string>(ALL_BLOCK_TYPES[0].value)
  const [addOrder, setAddOrder] = useState(blocks.length)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const effectiveAddType = allowedTypes.length > 0 && allowedTypes.some((t) => t.value === addType) ? addType : allowedTypes[0]?.value ?? addType

  const handleAdd = async () => {
    setError(null)
    setLoading(true)
    try {
      await coreApi.addContentBlock(projectId, contentId, {
        blockType: effectiveAddType,
        order: addOrder,
        payload: {},
      })
      onContentUpdated()
      setAddOrder(blocks.length + 1)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al añadir bloque')
    } finally {
      setLoading(false)
    }
  }

  const handleRemove = async (blockId: string) => {
    if (!window.confirm('¿Eliminar este bloque?')) return
    setError(null)
    setLoading(true)
    try {
      await coreApi.removeContentBlock(projectId, contentId, blockId)
      onContentUpdated()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al eliminar')
    } finally {
      setLoading(false)
    }
  }

  const handleMove = async (block: ContentBlock, direction: 'up' | 'down') => {
    const idx = blocks.findIndex((b) => b.id === block.id)
    if (idx < 0) return
    const newIdx = direction === 'up' ? idx - 1 : idx + 1
    if (newIdx < 0 || newIdx >= blocks.length) return
    const reordered = [...blocks]
    const [removed] = reordered.splice(idx, 1)
    reordered.splice(newIdx, 0, removed)
    const blockIds = reordered.map((b) => b.id)
    setError(null)
    setLoading(true)
    try {
      await coreApi.reorderContentBlocks(projectId, contentId, { blockIds })
      onContentUpdated()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al reordenar')
    } finally {
      setLoading(false)
    }
  }

  return (
    <section className="content-blocks-section" aria-labelledby="content-blocks-title">
      <h2 id="content-blocks-title" className="content-blocks-section__title">
        Bloques
      </h2>
      {blocks.length === 0 ? (
        <p className="content-blocks-section__empty">No hay bloques. Añade uno con el formulario inferior.</p>
      ) : (
        <ul className="content-blocks-section__list">
          {blocks.map((block) => (
            <li key={block.id} className="content-blocks-section__item">
              <span className="content-blocks-section__item-type">
                {ALL_BLOCK_TYPES.find((t) => t.value === block.blockType)?.label ?? block.blockType}
              </span>
              <span className="content-blocks-section__item-order">Orden: {block.order}</span>
              <div className="content-blocks-section__item-actions">
                <button
                  type="button"
                  className="content-blocks-section__btn"
                  onClick={() => handleMove(block, 'up')}
                  disabled={loading || blocks.indexOf(block) === 0}
                  aria-label="Subir bloque"
                >
                  ↑
                </button>
                <button
                  type="button"
                  className="content-blocks-section__btn"
                  onClick={() => handleMove(block, 'down')}
                  disabled={loading || blocks.indexOf(block) === blocks.length - 1}
                  aria-label="Bajar bloque"
                >
                  ↓
                </button>
                <button
                  type="button"
                  className="content-blocks-section__btn content-blocks-section__btn--danger"
                  onClick={() => handleRemove(block.id)}
                  disabled={loading}
                  aria-label="Eliminar bloque"
                >
                  Eliminar
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
      {allowedTypes.length > 0 ? (
        <div className="content-blocks-section__add">
          <div className="content-blocks-section__add-row">
            <label htmlFor="block-type-select">Tipo</label>
            <select
              id="block-type-select"
              value={effectiveAddType}
              onChange={(e) => setAddType(e.target.value)}
              disabled={loading}
            >
              {allowedTypes.map((t) => (
                <option key={t.value} value={t.value}>
                  {t.label}
                </option>
              ))}
            </select>
            <label htmlFor="block-order-input">Orden</label>
            <input
              id="block-order-input"
              type="number"
              min={0}
              value={addOrder}
              onChange={(e) => setAddOrder(parseInt(e.target.value, 10) || 0)}
              disabled={loading}
            />
            <button
              type="button"
              className="content-blocks-section__btn content-blocks-section__btn--primary"
              onClick={handleAdd}
              disabled={loading}
            >
              {loading ? '…' : 'Añadir bloque'}
            </button>
          </div>
          {error && <p className="content-blocks-section__error">{error}</p>}
        </div>
      ) : (
        <p className="content-blocks-section__empty">
          El schema de este contenido no permite bloques. Configura «Bloques permitidos» en el diseñador de schemas.
        </p>
      )}
    </section>
  )
}
