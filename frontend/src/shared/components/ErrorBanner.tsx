const styles: Record<string, React.CSSProperties> = {
  banner: {
    padding: '0.75rem 1rem',
    background: '#f8d7da',
    color: '#721c24',
    border: '1px solid #f5c6cb',
    borderRadius: 6,
    marginBottom: '1rem',
    fontSize: '0.875rem',
  },
  title: { fontWeight: 600, marginBottom: '0.25rem' },
  message: { margin: 0 },
}

/** Banner de error para mostrar mensajes de error de forma destacada. */
export function ErrorBanner({
  title = 'Error',
  message,
  onDismiss,
}: {
  title?: string
  message: string
  onDismiss?: () => void
}) {
  return (
    <div style={styles.banner}>
      <div style={styles.title}>{title}</div>
      <p style={styles.message}>{message}</p>
      {onDismiss && (
        <button
          type="button"
          onClick={onDismiss}
          style={{
            marginTop: '0.5rem',
            padding: '0.25rem 0.5rem',
            fontSize: '0.75rem',
            background: 'transparent',
            border: '1px solid #721c24',
            borderRadius: 4,
            cursor: 'pointer',
            color: '#721c24',
          }}
        >
          Cerrar
        </button>
      )}
    </div>
  )
}
