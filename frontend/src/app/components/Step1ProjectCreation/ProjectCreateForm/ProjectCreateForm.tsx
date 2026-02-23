import { X } from 'lucide-react'
import { ErrorBanner } from '../../../../shared/components'

export interface ProjectCreateFormProps {
  name: string
  description: string
  error: string | null
  saving: boolean
  onNameChange: (value: string) => void
  onDescriptionChange: (value: string) => void
  onSubmit: () => void
  /** Cancelar/cerrar creación (opcional; si se pasa, se muestra la tache) */
  onCancel?: () => void
  /** Prefijo para ids (ej. "step1" o "step1-desk") para evitar colisiones */
  idPrefix?: string
}

export function ProjectCreateForm({
  name,
  description,
  error,
  saving,
  onNameChange,
  onDescriptionChange,
  onSubmit,
  onCancel,
  idPrefix = 'step1',
}: ProjectCreateFormProps) {
  const nameId = `${idPrefix}-project-name`
  const descId = `${idPrefix}-project-desc`
  const titleId = `${idPrefix}-form-title`

  return (
    <div className="onboarding-step__form-wrap onboarding-step__form-wrap--centered">
      {onCancel && (
        <button
          type="button"
          className="onboarding-step__form-close"
          onClick={onCancel}
          disabled={saving}
          aria-label="Cancelar creación de proyecto"
        >
          <X size={20} strokeWidth={2} />
        </button>
      )}
      <h3 className="onboarding-step__form-title" id={titleId}>
        Datos del proyecto
      </h3>
      <form
        className="onboarding-step__form"
        onSubmit={(e) => {
          e.preventDefault()
          onSubmit()
        }}
        noValidate
        aria-labelledby={titleId}
      >
        <div className="onboarding-step__field">
          <label className="onboarding-step__label" htmlFor={nameId}>
            Nombre *
          </label>
          <input
            id={nameId}
            type="text"
            className="onboarding-step__input"
            value={name}
            onChange={(e) => onNameChange(e.target.value)}
            placeholder="Ej. Mi sitio"
            disabled={saving}
            required
            aria-invalid={!!error}
          />
        </div>
        <div className="onboarding-step__field">
          <label className="onboarding-step__label" htmlFor={descId}>
            Descripción (opcional)
          </label>
          <input
            id={descId}
            type="text"
            className="onboarding-step__input"
            value={description}
            onChange={(e) => onDescriptionChange(e.target.value)}
            placeholder="Breve descripción"
            disabled={saving}
          />
        </div>
        {error && <ErrorBanner message={error} />}
        <button type="submit" className="onboarding-step__submit" disabled={saving}>
          {saving ? 'Creando…' : 'Crear'}
        </button>
      </form>
    </div>
  )
}
