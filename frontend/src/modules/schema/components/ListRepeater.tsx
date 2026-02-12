import { useCallback } from 'react'

const styles: Record<string, React.CSSProperties> = {
  container: { marginTop: '0.25rem', color: 'var(--page-text)' },
  list: { listStyle: 'none', padding: 0, margin: '0.25rem 0 0 0' },
  row: {
    display: 'flex',
    gap: '0.5rem',
    alignItems: 'center',
    marginBottom: '0.5rem',
  },
  input: {
    flex: 1,
    maxWidth: 400,
    padding: '0.5rem',
    fontSize: '0.875rem',
    borderRadius: 4,
    border: '1px solid var(--input-border)',
    color: 'var(--input-text)',
    background: 'var(--input-bg)',
  },
  button: {
    padding: '0.35rem 0.6rem',
    fontSize: '0.8125rem',
    cursor: 'pointer',
    borderRadius: 4,
    border: '1px solid var(--input-border)',
    background: 'var(--input-bg)',
    color: 'var(--page-text)',
  },
  buttonAdd: { background: '#0d6efd', color: 'white', border: 'none' },
  buttonRemove: { background: '#dc3545', color: 'white', border: 'none', flexShrink: 0 },
  hint: { fontSize: '0.75rem', color: 'var(--page-text-muted)', marginTop: '0.25rem' },
}

export interface ListRepeaterProps {
  value: string[]
  onChange: (value: string[]) => void
  disabled?: boolean
  placeholder?: string
  helpText?: string | null
}

export function ListRepeater({
  value,
  onChange,
  disabled = false,
  placeholder = 'Nuevo valor',
  helpText,
}: ListRepeaterProps) {
  const items = Array.isArray(value) ? value : []

  const add = useCallback(() => {
    onChange([...items, ''])
  }, [items, onChange])

  const remove = useCallback(
    (index: number) => {
      onChange(items.filter((_, i) => i !== index))
    },
    [items, onChange]
  )

  const update = useCallback(
    (index: number, text: string) => {
      const next = [...items]
      next[index] = text
      onChange(next)
    },
    [items, onChange]
  )

  return (
    <div style={styles.container}>
      <div style={styles.row}>
        <button
          type="button"
          style={{ ...styles.button, ...styles.buttonAdd }}
          onClick={add}
          disabled={disabled}
        >
          + Añadir
        </button>
      </div>
      {items.length > 0 && (
        <ul style={styles.list}>
          {items.map((item, index) => (
            <li key={index} style={styles.row}>
              <input
                type="text"
                style={styles.input}
                value={item}
                onChange={(e) => update(index, e.target.value)}
                onBlur={() => {}}
                disabled={disabled}
                placeholder={placeholder}
              />
              <button
                type="button"
                style={{ ...styles.button, ...styles.buttonRemove }}
                onClick={() => remove(index)}
                disabled={disabled}
                title="Quitar"
              >
                −
              </button>
            </li>
          ))}
        </ul>
      )}
      {helpText && <p style={styles.hint}>{helpText}</p>}
    </div>
  )
}
