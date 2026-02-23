import type { ReactNode } from 'react'
import './AuthLayout.css'

interface AuthLayoutProps {
  children: ReactNode
}

/**
 * Layout compartido para login y registro: mobile-first, flexbox.
 * Panel izquierdo (móvil: ancho completo) + área de contenido; en desktop, spacer a la derecha.
 */
export function AuthLayout({ children }: AuthLayoutProps) {
  return (
    <main className="auth-layout" role="main">
      <section className="auth-layout__panel" aria-label="Formulario de acceso">
        <div className="auth-layout__brand" aria-hidden="true">
          ioda
        </div>
        <div className="auth-layout__content">
          {children}
        </div>
      </section>
      <div className="auth-layout__spacer" aria-hidden="true" />
    </main>
  )
}
