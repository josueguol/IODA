import type { ReactNode } from 'react'
import { OnboardingCard, getInitials } from '../../OnboardingCard'
import { EnvironmentCreateForm } from '../EnvironmentCreateForm'
import { ArrowLeft } from 'lucide-react'
import '../../OnboardingStep.css'

export interface EnvironmentItem {
  id: string
  name: string
  description?: string | null
}

export interface EnvironmentCardsSectionEmptyState {
  line1: string
  line2: string
  onOpenCreate: () => void
  ariaLabel?: string
}

export interface EnvironmentCardsSectionProps {
  title: string
  environments: EnvironmentItem[]
  currentEnvironmentId: string | null
  onSelectEnvironment: (id: string) => void
  /** Tarjeta/botón Crear entorno (se muestra en la fila de tarjetas o en estado vacío) */
  actions: ReactNode
  showCreate: boolean
  createName: string
  createDescription: string
  createError: string | null
  createSaving: boolean
  onCreateNameChange: (value: string) => void
  onCreateDescriptionChange: (value: string) => void
  onCreateSubmit: () => void
  sectionId?: string
  emptyState?: EnvironmentCardsSectionEmptyState
  onAddCardClick?: () => void
}

export function EnvironmentCardsSection({
  title,
  environments,
  currentEnvironmentId,
  onSelectEnvironment,
  actions,
  showCreate,
  createName,
  createDescription,
  createError,
  createSaving,
  onCreateNameChange,
  onCreateDescriptionChange,
  onCreateSubmit,
  sectionId = 'step2',
  emptyState,
  onAddCardClick,
}: EnvironmentCardsSectionProps) {
  return (
    <section
      className="onboarding-step onboarding-step--work-area"
      aria-labelledby={title ? `${sectionId}-title` : undefined}
    >
      {title ? (
        <h2 id={`${sectionId}-title`} className="onboarding-step__title">
          {title}
        </h2>
      ) : null}

      <>
        <div className="work-area work-area--empty">
          {environments.length > 0 && (
            <>
              {environments.map((e) => (
                <OnboardingCard
                  key={e.id}
                  initials={getInitials(e.name)}
                  title={e.name}
                  selected={currentEnvironmentId === e.id}
                  onClick={() => onSelectEnvironment(e.id)}
                  id={e.id === currentEnvironmentId ? `${sectionId}-selected-environment` : undefined}
                />
              ))}
            </>
          )}
          <div className="work-area__first-action">
            {onAddCardClick && actions && (
              <div className="onboarding-step__actions">{actions}</div>
            )}
            {emptyState && environments.length === 0 && !showCreate && (
              <>
                <span className="work-area__helper" aria-hidden>
                  <ArrowLeft size={28} strokeWidth={2} className="work-area__helper-arrow" />
                  <span className="work-area__helper-text">{emptyState.line2}</span>
                </span>
              </>
            )}
          </div>
          {emptyState && environments.length === 0 && !showCreate && (
            <p className="work-area__center-message">{emptyState.line1}</p>
          )}
        </div>
      </>

      {showCreate && (
        <EnvironmentCreateForm
          idPrefix={sectionId}
          name={createName}
          description={createDescription}
          error={createError}
          saving={createSaving}
          onNameChange={onCreateNameChange}
          onDescriptionChange={onCreateDescriptionChange}
          onSubmit={onCreateSubmit}
          onCancel={onAddCardClick}
        />
      )}
    </section>
  )
}
