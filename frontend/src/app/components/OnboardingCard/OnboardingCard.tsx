/**
 * Obtiene hasta 2 iniciales desde un nombre (ej. "Rex Salem" → "RS").
 * Exportado para uso en Steps sin duplicar lógica.
 */
export function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return '?'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

/**
 * Tarjeta reutilizable para Steps (proyecto, entorno, sitio).
 * Diseño: login.pen — card 192×192, border-radius 25px, iniciales 64px, nombre 16px/600.
 */
export interface OnboardingCardProps {
  /** Iniciales mostradas en el centro (ej. "RS", "D") */
  initials: string
  /** Nombre mostrado debajo de las iniciales */
  title: string
  /** Si está seleccionada: borde #35a0e8 y texto primary */
  selected?: boolean
  /** Callback al hacer clic */
  onClick: () => void
  /** id para accesibilidad */
  id?: string
}

export function OnboardingCard({
  initials,
  title,
  selected = false,
  onClick,
  id,
}: OnboardingCardProps) {
  return (
    <div
      id={id}
      className={`onboarding-card ${selected ? 'onboarding-card--selected' : ''}`}
      onClick={onClick}
      aria-pressed={selected}
      aria-label={`Seleccionar: ${title}`}
    >
      <span className="onboarding-card__initials" aria-hidden>
        {initials}
      </span>
      <span className="onboarding-card__title">{title}</span>
    </div>
  )
}
