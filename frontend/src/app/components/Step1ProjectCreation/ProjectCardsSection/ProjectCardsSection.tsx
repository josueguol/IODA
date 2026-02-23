import type { ReactNode } from 'react'
import { OnboardingCard, getInitials } from '../../OnboardingCard'
import { ProjectCreateForm } from '../ProjectCreateForm'
import { ArrowLeft } from 'lucide-react'
import '../../OnboardingStep.css'

export interface ProjectItem {
  id: string
  name: string
  description?: string | null
}

export interface ProjectCardsSectionEmptyState {
  line1: string
  line2: string
  onOpenCreate: () => void
  ariaLabel?: string
}

export interface ProjectCardsSectionProps {
  title: string
  projects: ProjectItem[]
  currentProjectId: string | null
  onSelectProject: (id: string) => void
  /** Botón(es) Crear proyecto / Crear primer proyecto */
  actions: ReactNode
  /** Si se muestra el formulario de creación */
  showCreate: boolean
  createName: string
  createDescription: string
  createError: string | null
  createSaving: boolean
  onCreateNameChange: (value: string) => void
  onCreateDescriptionChange: (value: string) => void
  onCreateSubmit: () => void
  /** Para ids únicos cuando hay varias secciones en página */
  sectionId?: string
  /** Cuando no hay proyectos y no se está creando: muestra tarjeta vacía + texto */
  emptyState?: ProjectCardsSectionEmptyState
  /** Si hay proyectos: muestra tarjeta "Crear proyecto" a la derecha en la misma fila; callback al hacer clic */
  onAddCardClick?: () => void
}

export function ProjectCardsSection({
  title,
  projects,
  currentProjectId,
  onSelectProject,
  actions,
  showCreate,
  createName,
  createDescription,
  createError,
  createSaving,
  onCreateNameChange,
  onCreateDescriptionChange,
  onCreateSubmit,
  sectionId = 'step1',
  emptyState,
  onAddCardClick,
}: ProjectCardsSectionProps) {
  return (
    <section
      className="onboarding-step onboarding-step--work-area"
      aria-labelledby={title ? `${sectionId}-title` : undefined}
    >
        <h2 id={`${sectionId}-title`} className="onboarding-step__title">
          {title}
        </h2>

      <>

        <div className="work-area work-area--empty">
          {projects.length > 0 && (
            <>
              {projects.map((p) => (
                  <OnboardingCard
                    key={p.id}
                    initials={getInitials(p.name)}
                    title={p.name}
                    selected={currentProjectId === p.id}
                    onClick={() => onSelectProject(p.id)}
                    id={p.id === currentProjectId ? `${sectionId}-selected-project` : undefined}
                  />
              ))}
            </>
          )}
          <div className="work-area__first-action">
            {onAddCardClick && actions && (
              <div className="onboarding-step__actions">{actions}</div>
            )}
            {emptyState && projects.length === 0 && !showCreate && (
              <>
                <span className="work-area__helper" aria-hidden>
                  <ArrowLeft size={28} strokeWidth={2} className="work-area__helper-arrow" />
                  <span className="work-area__helper-text">{emptyState.line2}</span>
                </span>
              </>
            )}
          </div>
          {emptyState && projects.length === 0 && !showCreate && (
            <p className="work-area__center-message">{emptyState.line1}</p>
          )}
        </div>
      </>

      {showCreate && (
        <ProjectCreateForm
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
