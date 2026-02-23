import type { ReactNode } from 'react'
import { LoadingSpinner } from '../../../shared/components'
import { EnvironmentCardsSection } from './EnvironmentCardsSection'
import '../OnboardingStep.css'

export interface EnvironmentItem {
  id: string
  name: string
  description?: string | null
}

export interface Step2EnvironmentSelectionProps {
  environments: EnvironmentItem[]
  loading: boolean
  showCreate: boolean
  currentEnvironmentId: string | null
  newEnvName: string
  newEnvDesc: string
  createError: string | null
  saving: boolean
  onSelectEnvironment: (id: string) => void
  onNameChange: (value: string) => void
  onDescChange: (value: string) => void
  onSubmit: () => void
  actions: ReactNode
  onOpenCreate?: () => void
}

export function Step2EnvironmentSelection({
  environments,
  loading,
  showCreate,
  currentEnvironmentId,
  newEnvName,
  newEnvDesc,
  createError,
  saving,
  onSelectEnvironment,
  onNameChange,
  onDescChange,
  onSubmit,
  actions,
  onOpenCreate,
}: Step2EnvironmentSelectionProps) {
  const title = 'Entornos'

  if (loading && environments.length === 0) {
    return (
      <section className="onboarding-step onboarding-step--clean" aria-labelledby="step2-title">
        <h2 id="step2-title" className="onboarding-step__title">
          {title}
        </h2>
        <LoadingSpinner text="Cargando entornos…" />
      </section>
    )
  }

  return (
    <EnvironmentCardsSection
      sectionId="step2"
      title={title}
      environments={environments}
      currentEnvironmentId={currentEnvironmentId}
      onSelectEnvironment={onSelectEnvironment}
      actions={actions}
      showCreate={showCreate}
      createName={newEnvName}
      createDescription={newEnvDesc}
      createError={createError}
      createSaving={saving}
      onCreateNameChange={onNameChange}
      onCreateDescriptionChange={onDescChange}
      onCreateSubmit={onSubmit}
      onAddCardClick={onOpenCreate}
      emptyState={
        !loading && environments.length === 0 && onOpenCreate
          ? {
              line1: 'No hay entornos todavía.',
              line2: 'Crea tu primer entorno.',
              onOpenCreate,
              ariaLabel: 'Crear entorno',
            }
          : undefined
      }
    />
  )
}
