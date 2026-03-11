import type { ToolbarState } from './formattedTextEditor.utils'

interface FormattedTextEditorToolbarProps {
  toolbarState: ToolbarState
  onSaveSelection: () => void
  onToggleTag: (tagName: 'strong' | 'em' | 'u' | 's') => void
  onOpenColor: () => void
  onOpenBackground: () => void
  onOpenLink: () => void
  onRemoveLink: () => void
}

function ToolbarButton({
  active = false,
  disabled = false,
  triggerId,
  label,
  title,
  onMouseDown,
  onClick,
}: {
  active?: boolean
  disabled?: boolean
  triggerId?: 'textColor' | 'backgroundColor' | 'link'
  label: string
  title: string
  onMouseDown: () => void
  onClick: () => void
}) {
  return (
    <button
      type="button"
      className={`formatted-text-editor__button${active ? ' formatted-text-editor__button--active' : ''}`}
      data-overlay-trigger={triggerId}
      title={title}
      disabled={disabled}
      onMouseDown={(event) => {
        event.preventDefault()
        onMouseDown()
      }}
      onClick={onClick}
    >
      {label}
    </button>
  )
}

export function FormattedTextEditorToolbar({
  toolbarState,
  onSaveSelection,
  onToggleTag,
  onOpenColor,
  onOpenBackground,
  onOpenLink,
  onRemoveLink,
}: FormattedTextEditorToolbarProps) {
  return (
    <div className="formatted-text-editor__toolbar">
      <ToolbarButton active={toolbarState.bold} label="B" title="Bold" onMouseDown={onSaveSelection} onClick={() => onToggleTag('strong')} />
      <ToolbarButton active={toolbarState.italic} label="I" title="Italic" onMouseDown={onSaveSelection} onClick={() => onToggleTag('em')} />
      <ToolbarButton active={toolbarState.underline} label="U" title="Underline" onMouseDown={onSaveSelection} onClick={() => onToggleTag('u')} />
      <ToolbarButton active={toolbarState.strike} label="S" title="Strikethrough" onMouseDown={onSaveSelection} onClick={() => onToggleTag('s')} />
      <ToolbarButton active={toolbarState.hasExplicitColor} triggerId="textColor" label="Color" title="Text Color" onMouseDown={onSaveSelection} onClick={onOpenColor} />
      <ToolbarButton active={toolbarState.hasExplicitBackground} triggerId="backgroundColor" label="Fondo" title="Background Color" onMouseDown={onSaveSelection} onClick={onOpenBackground} />
      <ToolbarButton active={!!toolbarState.linkHref} triggerId="link" label="Link" title="Add Link" onMouseDown={onSaveSelection} onClick={onOpenLink} />
      {toolbarState.linkHref && (
        <ToolbarButton label="Unlink" title="Remove Link" onMouseDown={onSaveSelection} onClick={onRemoveLink} />
      )}
    </div>
  )
}
