/**
 * Tarjeta vacía para "crear primer elemento" (proyecto/entorno/sitio).
 * Diseño: login.pen — card 192×192, border-radius 25px, icono plus, dos líneas de texto.
 */
export interface OnboardingEmptyCardProps {
  /** Primera línea (ej. "No hay proyectos todavía.") */
  line1: string
  /** Segunda línea (ej. "Crea tu primer proyecto.") */
  line2: string
  onClick: () => void
  ariaLabel?: string
}

export function OnboardingEmptyCard({
  line1,
  line2,
  onClick,
  ariaLabel = 'Crear nuevo',
}: OnboardingEmptyCardProps) {
  return (
    <div className="onboarding-empty">
      <button
        type="button"
        className="onboarding-empty__card"
        onClick={onClick}
        aria-label={ariaLabel}
      >
        <span className="onboarding-empty__icon" aria-hidden>
          +
        </span>
      </button>
      <p className="onboarding-empty__text">
        <span>{line1}</span>
        <span>{line2}</span>
      </p>
    </div>
  )
}
