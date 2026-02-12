import { Link, useLocation } from 'react-router-dom'

const styles: Record<string, React.CSSProperties> = {
  container: {
    padding: '2rem',
    fontFamily: 'system-ui, sans-serif',
    maxWidth: 480,
    margin: '4rem auto',
    textAlign: 'center',
    color: 'var(--page-text)',
  },
  title: { color: '#856404', marginBottom: '1rem' },
  link: { color: '#0d6efd', textDecoration: 'none' },
}

export function ForbiddenPage() {
  const location = useLocation()
  const state = location.state as { permission?: string } | undefined
  const permission = state?.permission

  return (
    <div style={styles.container}>
      <h1 style={styles.title}>Sin permiso</h1>
      <p>
        No tienes permiso para acceder a esta página
        {permission ? ` (se requiere: ${permission})` : ''}.
      </p>
      <p>
        <Link to="/" style={styles.link}>
          Volver al inicio
        </Link>
      </p>
    </div>
  )
}
