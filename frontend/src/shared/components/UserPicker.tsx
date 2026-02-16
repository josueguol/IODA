import { useState, useRef, useEffect } from 'react'
import type { UserListItemDto } from '../../modules/auth/types'

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i

function getLabel(u: UserListItemDto): string {
  const name = u.displayName?.trim()
  if (name) return `${name} (${u.email})`
  return u.email
}

function filterUsers(users: UserListItemDto[], query: string): UserListItemDto[] {
  const q = query.trim().toLowerCase()
  if (!q) return users.slice(0, 20)
  return users.filter(
    (u) =>
      u.email.toLowerCase().includes(q) ||
      (u.displayName ?? '').toLowerCase().includes(q) ||
      u.id.toLowerCase().includes(q)
  ).slice(0, 20)
}

const styles: Record<string, React.CSSProperties> = {
  wrap: { position: 'relative', maxWidth: 360 },
  input: {
    width: '100%',
    padding: '0.5rem',
    fontSize: '0.875rem',
    borderRadius: 4,
    border: '1px solid var(--input-border)',
    color: 'var(--input-text)',
    background: 'var(--input-bg)',
  },
  row: { display: 'flex', alignItems: 'center', gap: '0.5rem' },
  changeBtn: {
    background: 'none',
    border: 'none',
    color: '#0d6efd',
    cursor: 'pointer',
    fontSize: '0.8125rem',
    padding: 0,
    textDecoration: 'underline',
  },
  dropdown: {
    position: 'absolute',
    top: '100%',
    left: 0,
    right: 0,
    maxHeight: 220,
    overflowY: 'auto',
    background: 'var(--page-bg-elevated)',
    border: '1px solid var(--page-border)',
    borderRadius: 4,
    boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
    zIndex: 10,
    marginTop: 2,
  },
  option: {
    padding: '0.5rem 0.75rem',
    cursor: 'pointer',
    fontSize: '0.875rem',
    borderBottom: '1px solid var(--page-border)',
    color: 'var(--page-text)',
  },
  optionLast: { borderBottom: 'none' },
}

export interface UserPickerProps {
  users: UserListItemDto[]
  value: string
  onChange: (userId: string) => void
  placeholder?: string
  id?: string
  /** Si true, al pegar un GUID válido se acepta aunque no esté en la lista. */
  allowRawGuid?: boolean
}

/**
 * Buscador/selector de usuario por correo, nombre o ID.
 * Muestra dropdown filtrado; permite pegar un GUID directamente (casos avanzados).
 */
export function UserPicker({
  users,
  value,
  onChange,
  placeholder = 'Buscar por correo, nombre o ID…',
  id,
  allowRawGuid = true,
}: UserPickerProps) {
  const [query, setQuery] = useState('')
  const [open, setOpen] = useState(false)
  const wrapperRef = useRef<HTMLDivElement>(null)

  const selectedUser = value ? users.find((u) => u.id === value) : null
  const displayLabel = selectedUser ? getLabel(selectedUser) : value || ''
  const filtered = filterUsers(users, query)

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (wrapperRef.current && !wrapperRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const handleBlur = () => {
    if (!allowRawGuid) return
    const trimmed = query.trim()
    if (UUID_REGEX.test(trimmed)) {
      onChange(trimmed)
      setQuery('')
    }
    setOpen(false)
  }

  const handleSelect = (userId: string) => {
    onChange(userId)
    setQuery('')
    setOpen(false)
  }

  const handleClear = () => {
    onChange('')
    setQuery('')
    setOpen(false)
  }

  return (
    <div ref={wrapperRef} style={styles.wrap}>
      <div style={styles.row}>
        <input
          id={id}
          type="text"
          style={{ ...styles.input, flex: 1 }}
          value={open ? (query || (selectedUser ? getLabel(selectedUser) : '')) : (selectedUser ? getLabel(selectedUser) : query)}
          onChange={(e) => {
            setQuery(e.target.value)
            setOpen(true)
          }}
          onFocus={() => setOpen(true)}
          onBlur={handleBlur}
          placeholder={value ? '' : placeholder}
          autoComplete="off"
          aria-autocomplete="list"
          aria-expanded={open}
        />
        {value && !open && (
          <button type="button" style={styles.changeBtn} onClick={handleClear}>
            Cambiar
          </button>
        )}
      </div>
      {open && filtered.length > 0 && (
        <div style={styles.dropdown} role="listbox">
          {filtered.map((u, i) => (
            <div
              key={u.id}
              role="option"
              style={{ ...styles.option, ...(i === filtered.length - 1 ? styles.optionLast : {}) }}
              onMouseDown={(e) => {
                e.preventDefault()
                handleSelect(u.id)
              }}
            >
              {getLabel(u)}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
