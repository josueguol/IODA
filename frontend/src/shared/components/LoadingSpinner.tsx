const styles: Record<string, React.CSSProperties> = {
  container: { display: 'flex', alignItems: 'center', gap: '0.5rem', padding: '1rem' },
  spinner: {
    width: 20,
    height: 20,
    border: '3px solid #f3f3f3',
    borderTop: '3px solid #0d6efd',
    borderRadius: '50%',
    animation: 'spin 1s linear infinite',
  },
  text: { color: '#666', fontSize: '0.875rem' },
}

/** Spinner de carga simple. */
export function LoadingSpinner({ text = 'Cargando…' }: { text?: string }) {
  return (
    <div style={styles.container}>
      <div style={styles.spinner} />
      {text && <span style={styles.text}>{text}</span>}
    </div>
  )
}
