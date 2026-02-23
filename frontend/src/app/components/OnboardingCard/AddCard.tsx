/**
 * Tarjeta genérica "agregar" para el área de trabajo (Step 1, 2, 3): gris, icono + y leyenda.
 * Se muestra junto a las tarjetas de proyecto/entorno/sitio en la misma fila.
 */
import { Plus } from 'lucide-react'

export interface AddCardProps {
  onClick: () => void
  ariaLabel?: string
  /** Leyenda bajo el icono (ej. "Crear proyecto", "Crear entorno", "Crear sitio") */
  legend?: string
}

export function AddCard({
  onClick,
  ariaLabel = 'Agregar',
  legend = 'Agregar',
}: AddCardProps) {
  return (
    <div
      className="onboarding-add-card"
      onClick={onClick}
      aria-label={ariaLabel}
    >
      <span className="onboarding-add-card__icon" aria-hidden>
        <Plus size={48} strokeWidth={2} />
      </span>
      <span className="onboarding-add-card__legend">{legend}</span>
    </div>
  )
}
