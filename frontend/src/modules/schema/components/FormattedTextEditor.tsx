import { useEffect, useRef, useState } from 'react'
import './FormattedTextEditor.css'
import { FormattedTextEditorToolbar } from './FormattedTextEditorToolbar'
import {
  BACKGROUND_SWATCHES,
  TEXT_SWATCHES,
  type OverlayState,
  type ToolbarState,
  findAncestorTag,
  findStyledAncestor,
  focusEditor,
  getCurrentRange,
  getToolbarState,
  restoreRange,
  unwrapElement,
  wrapSelection,
} from './formattedTextEditor.utils'

interface FormattedTextEditorProps {
  value: unknown
  onChange: (nextValue: string) => void
}

const CLOSED_OVERLAY: OverlayState = { type: 'none' }

export function FormattedTextEditor({ value, onChange }: FormattedTextEditorProps) {
  const rootRef = useRef<HTMLDivElement | null>(null)
  const editorRef = useRef<HTMLDivElement | null>(null)
  const savedRangeRef = useRef<Range | null>(null)
  const [toolbarState, setToolbarState] = useState<ToolbarState>(() => getToolbarState(null))
  const [overlay, setOverlay] = useState<OverlayState>(CLOSED_OVERLAY)

  const isLinkInputOpen = overlay.type === 'link'
  const isColorPickerOpen = overlay.type === 'textColor'
  const isBackgroundPickerOpen = overlay.type === 'backgroundColor'

  const syncToolbar = () => setToolbarState(getToolbarState(editorRef.current))
  const saveSelection = () => {
    const range = getCurrentRange(editorRef.current)
    savedRangeRef.current = range ? range.cloneRange() : null
  }
  const restoreSelection = () => restoreRange(savedRangeRef.current)
  const closeOverlay = () => setOverlay(CLOSED_OVERLAY)
  const commitChange = () => onChange(editorRef.current?.innerHTML ?? '')
  const returnFocusToEditor = () => {
    restoreSelection()
    focusEditor(editorRef.current)
    syncToolbar()
  }

  useEffect(() => {
    const editor = editorRef.current
    const nextValue = typeof value === 'string' ? value : ''
    if (editor && editor.innerHTML !== nextValue) {
      editor.innerHTML = nextValue
    }
  }, [value])

  useEffect(() => {
    const onSelectionChange = () => syncToolbar()
    const onMouseDown = (event: MouseEvent) => {
      const root = rootRef.current
      if (!root) return
      if (event.target instanceof Node && !root.contains(event.target)) {
        closeOverlay()
      }
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        closeOverlay()
        returnFocusToEditor()
      }
    }

    document.addEventListener('selectionchange', onSelectionChange)
    document.addEventListener('mousedown', onMouseDown)
    document.addEventListener('keydown', onKeyDown)

    return () => {
      document.removeEventListener('selectionchange', onSelectionChange)
      document.removeEventListener('mousedown', onMouseDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [])

  const openOverlay = (type: OverlayState['type']) => {
    const root = rootRef.current
    const range = getCurrentRange(editorRef.current)
    if (!root || type === 'none') return
    if (type === 'link' && (!range || range.collapsed)) return

    const anchor = root.querySelector<HTMLElement>(`[data-overlay-trigger="${type}"]`)
    const anchorRect = anchor?.getBoundingClientRect() ?? range?.getBoundingClientRect()
    const rootRect = root.getBoundingClientRect()
    const top = (anchorRect?.bottom ?? rootRect.top) - rootRect.top + 8
    const left = Math.max(0, (anchorRect?.left ?? rootRect.left) - rootRect.left)

    if (type === 'link') {
      const currentLink = findAncestorTag(range?.commonAncestorContainer ?? null, editorRef.current, ['a'])
      setOverlay({ type, top, left, url: currentLink?.getAttribute('href') ?? '' })
      return
    }

    setOverlay({ type, top, left })
  }

  const toggleInlineTag = (tagName: 'strong' | 'em' | 'u' | 's') => {
    const editor = editorRef.current
    if (!editor) return

    restoreSelection()
    const range = getCurrentRange(editor)
    if (!range || range.collapsed) return

    const ancestor = findAncestorTag(range.commonAncestorContainer, editor, [tagName])
    if (ancestor) {
      unwrapElement(ancestor)
    } else {
      wrapSelection(range, document.createElement(tagName))
    }

    commitChange()
    closeOverlay()
    returnFocusToEditor()
  }

  const applyStyle = (styleProperty: 'color' | 'backgroundColor', value: string | null) => {
    const editor = editorRef.current
    if (!editor) return

    restoreSelection()
    const range = getCurrentRange(editor)
    if (!range || range.collapsed) return

    const ancestor = findStyledAncestor(range.commonAncestorContainer, editor, styleProperty)
    if (value === null) {
      if (!ancestor) return
      ancestor.style.removeProperty(styleProperty === 'color' ? 'color' : 'background-color')
      if (ancestor.tagName.toLowerCase() === 'span' && !ancestor.getAttribute('style')?.trim()) {
        unwrapElement(ancestor)
      }
    } else {
      const wrapper = document.createElement('span')
      wrapper.style[styleProperty] = value
      wrapSelection(range, wrapper)
    }

    commitChange()
    closeOverlay()
    returnFocusToEditor()
  }

  const applyLink = () => {
    if (overlay.type !== 'link') return
    const editor = editorRef.current
    if (!editor) return

    restoreSelection()
    const range = getCurrentRange(editor)
    const href = overlay.url.trim()
    if (!range || range.collapsed || !href) return

    const existingLink = findAncestorTag(range.commonAncestorContainer, editor, ['a'])
    if (existingLink) {
      existingLink.setAttribute('href', href)
      existingLink.setAttribute('target', '_blank')
      existingLink.setAttribute('rel', 'noopener noreferrer')
    } else {
      const wrapper = document.createElement('a')
      wrapper.href = href
      wrapper.target = '_blank'
      wrapper.rel = 'noopener noreferrer'
      wrapSelection(range, wrapper)
    }

    commitChange()
    closeOverlay()
    returnFocusToEditor()
  }

  const removeLink = () => {
    const editor = editorRef.current
    if (!editor) return

    restoreSelection()
    const range = getCurrentRange(editor)
    if (!range) return

    const link = findAncestorTag(range.commonAncestorContainer, editor, ['a'])
    if (!link) return

    unwrapElement(link)
    commitChange()
    closeOverlay()
    returnFocusToEditor()
  }

  return (
    <div ref={rootRef} className="formatted-text-editor">
      <FormattedTextEditorToolbar
        toolbarState={toolbarState}
        onSaveSelection={saveSelection}
        onToggleTag={toggleInlineTag}
        onOpenColor={() => openOverlay('textColor')}
        onOpenBackground={() => openOverlay('backgroundColor')}
        onOpenLink={() => openOverlay('link')}
        onRemoveLink={removeLink}
      />

      {isLinkInputOpen && overlay.type === 'link' && (
        <div className="formatted-text-editor__popover" style={{ top: overlay.top, left: overlay.left }}>
          <input
            type="url"
            className="formatted-text-editor__link-input"
            placeholder="https://..."
            value={overlay.url}
            autoFocus
            onChange={(event) => setOverlay({ ...overlay, url: event.target.value })}
          />
          <button type="button" className="formatted-text-editor__button" onClick={applyLink}>Aceptar</button>
          <button type="button" className="formatted-text-editor__button" onClick={() => { closeOverlay(); returnFocusToEditor() }}>Cancelar</button>
        </div>
      )}

      {(isColorPickerOpen || isBackgroundPickerOpen) && (
        <div className="formatted-text-editor__popover" style={{ top: overlay.top, left: overlay.left }}>
          <button
            type="button"
            className="formatted-text-editor__swatch formatted-text-editor__swatch--auto"
            onClick={() => applyStyle(overlay.type === 'textColor' ? 'color' : 'backgroundColor', null)}
          >
            Auto
          </button>
          {(overlay.type === 'textColor' ? TEXT_SWATCHES : BACKGROUND_SWATCHES).map((swatch) => (
            <button
              key={swatch}
              type="button"
              className="formatted-text-editor__swatch"
              style={{ backgroundColor: swatch }}
              onClick={() => applyStyle(overlay.type === 'textColor' ? 'color' : 'backgroundColor', swatch)}
              aria-label={swatch}
              title={swatch}
            />
          ))}
        </div>
      )}

      <div
        ref={editorRef}
        className="formatted-text-editor__surface"
        contentEditable
        role="textbox"
        aria-multiline="true"
        suppressContentEditableWarning
        onInput={(event) => onChange(event.currentTarget.innerHTML)}
        onBlur={(event) => onChange(event.currentTarget.innerHTML)}
        onMouseUp={() => {
          saveSelection()
          syncToolbar()
        }}
        onKeyUp={() => {
          saveSelection()
          syncToolbar()
        }}
        onKeyDown={(event) => {
          if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault()
            document.execCommand('insertLineBreak')
            onChange(event.currentTarget.innerHTML)
          }
        }}
        onPaste={(event) => {
          event.preventDefault()
          const text = event.clipboardData.getData('text/plain')
          document.execCommand('insertText', false, text)
        }}
      />
    </div>
  )
}
