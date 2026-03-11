export interface ToolbarState {
  bold: boolean
  italic: boolean
  underline: boolean
  strike: boolean
  color: string
  hasExplicitColor: boolean
  backgroundColor: string
  hasExplicitBackground: boolean
  linkHref: string | null
}

export type OverlayState =
  | { type: 'none' }
  | { type: 'link'; top: number; left: number; url: string }
  | { type: 'textColor'; top: number; left: number }
  | { type: 'backgroundColor'; top: number; left: number }

export const DEFAULT_TEXT_COLOR = '#000000'
export const DEFAULT_BACKGROUND_COLOR = '#ffffff'

export const TEXT_SWATCHES = ['#111827', '#dc2626', '#ea580c', '#ca8a04', '#16a34a', '#0891b2', '#2563eb', '#7c3aed']
export const BACKGROUND_SWATCHES = ['#f3f4f6', '#fee2e2', '#ffedd5', '#fef3c7', '#dcfce7', '#cffafe', '#dbeafe', '#ede9fe']

export function isSelectionInside(editor: HTMLDivElement | null, selection: Selection | null): boolean {
  if (!editor || !selection || selection.rangeCount === 0) return false
  const anchorNode = selection.anchorNode
  const focusNode = selection.focusNode
  return !!anchorNode && !!focusNode && editor.contains(anchorNode) && editor.contains(focusNode)
}

export function getCurrentRange(editor: HTMLDivElement | null): Range | null {
  const selection = window.getSelection()
  if (!editor || !selection || selection.rangeCount === 0) return null
  if (!isSelectionInside(editor, selection)) return null
  return selection.getRangeAt(0)
}

export function restoreRange(range: Range | null) {
  if (!range) return
  const selection = window.getSelection()
  if (!selection) return
  selection.removeAllRanges()
  selection.addRange(range)
}

export function unwrapElement(element: HTMLElement) {
  const parent = element.parentNode
  if (!parent) return
  while (element.firstChild) {
    parent.insertBefore(element.firstChild, element)
  }
  parent.removeChild(element)
}

export function findAncestorTag(node: Node | null, editor: HTMLDivElement | null, tags: string[]): HTMLElement | null {
  let current: Node | null = node
  while (current && current !== editor) {
    if (current instanceof HTMLElement && tags.includes(current.tagName.toLowerCase())) {
      return current
    }
    current = current.parentNode
  }
  return null
}

export function findStyledAncestor(
  node: Node | null,
  editor: HTMLDivElement | null,
  styleProperty: 'color' | 'backgroundColor',
): HTMLElement | null {
  let current: Node | null = node
  while (current && current !== editor) {
    if (current instanceof HTMLElement && current.style[styleProperty]) {
      return current
    }
    current = current.parentNode
  }
  return null
}

export function wrapSelection(range: Range, wrapper: HTMLElement): HTMLElement | null {
  const fragment = range.extractContents()
  if (!fragment.textContent?.trim() && !fragment.querySelector('*')) return null
  wrapper.appendChild(fragment)
  range.insertNode(wrapper)

  const selection = window.getSelection()
  if (selection) {
    selection.removeAllRanges()
    const nextRange = document.createRange()
    nextRange.selectNodeContents(wrapper)
    selection.addRange(nextRange)
  }

  return wrapper
}

export function rgbToHex(color: string, fallback: string): string {
  if (!color) return fallback
  if (color.startsWith('#')) return color

  const match = color.match(/\d+/g)
  if (!match || match.length < 3) return fallback

  const [r, g, b] = match.slice(0, 3).map(Number)
  return `#${[r, g, b].map((value) => value.toString(16).padStart(2, '0')).join('')}`
}

export function getToolbarState(editor: HTMLDivElement | null): ToolbarState {
  const selection = window.getSelection()
  if (!editor || !isSelectionInside(editor, selection)) {
    return {
      bold: false,
      italic: false,
      underline: false,
      strike: false,
      color: DEFAULT_TEXT_COLOR,
      hasExplicitColor: false,
      backgroundColor: DEFAULT_BACKGROUND_COLOR,
      hasExplicitBackground: false,
      linkHref: null,
    }
  }

  const anchorNode = selection?.anchorNode ?? null
  const element = anchorNode instanceof HTMLElement ? anchorNode : anchorNode?.parentElement ?? null
  const computed = element ? window.getComputedStyle(element) : null
  const colorTag = findStyledAncestor(element, editor, 'color')
  const backgroundTag = findStyledAncestor(element, editor, 'backgroundColor')
  const explicitColor = colorTag?.style.color?.trim() ?? ''
  const explicitBackground = backgroundTag?.style.backgroundColor?.trim() ?? ''

  return {
    bold: !!findAncestorTag(element, editor, ['strong', 'b']) || Number(computed?.fontWeight ?? 400) >= 600,
    italic: !!findAncestorTag(element, editor, ['em', 'i']) || (computed?.fontStyle ?? '').includes('italic'),
    underline: !!findAncestorTag(element, editor, ['u']) || (computed?.textDecorationLine ?? '').includes('underline'),
    strike: !!findAncestorTag(element, editor, ['s', 'strike']) || (computed?.textDecorationLine ?? '').includes('line-through'),
    color: rgbToHex(explicitColor, DEFAULT_TEXT_COLOR),
    hasExplicitColor: !!explicitColor,
    backgroundColor: rgbToHex(explicitBackground, DEFAULT_BACKGROUND_COLOR),
    hasExplicitBackground: !!explicitBackground,
    linkHref: findAncestorTag(element, editor, ['a'])?.getAttribute('href') ?? null,
  }
}

export function focusEditor(editor: HTMLDivElement | null) {
  if (!editor) return
  editor.focus()
}
