import type { ReactNode } from 'react'
import { LoadingSpinner } from '../../../shared/components'
import { ProjectCardsSection } from './ProjectCardsSection'
import '../OnboardingStep.css'

export interface ProjectItem {
  id: string
  name: string
  description?: string | null
}

export interface Step1ProjectCreationProps {
  projects: ProjectItem[]
  hasNoProjects: boolean
  loading: boolean
  showCreate: boolean
  currentProjectId: string | null
  newProjectName: string
  newProjectDesc: string
  createError: string | null
  saving: boolean
  onSelectProject: (id: string) => void
  onNameChange: (value: string) => void
  onDescChange: (value: string) => void
  onSubmit: () => void
  actions: ReactNode
  onOpenCreate?: () => void
}

export function Step1ProjectCreation({
  projects,
  hasNoProjects,
  loading,
  showCreate,
  currentProjectId,
  newProjectName,
  newProjectDesc,
  createError,
  saving,
  onSelectProject,
  onNameChange,
  onDescChange,
  onSubmit,
  actions,
  onOpenCreate,
}: Step1ProjectCreationProps) {
  const title = 'Proyectos'

  if (loading && projects.length === 0) {
    return (
      <section className="onboarding-step" aria-labelledby="step1-title">
        <h2 id="step1-title" className="onboarding-step__title">
          {title}
        </h2>
        <LoadingSpinner text="Cargando proyectos…" />
      </section>
    )
  }

  return (
    <ProjectCardsSection
      sectionId="step1"
      title={title}
      projects={projects}
      currentProjectId={currentProjectId}
      onSelectProject={onSelectProject}
      actions={actions}
      showCreate={showCreate}
      createName={newProjectName}
      createDescription={newProjectDesc}
      createError={createError}
      createSaving={saving}
      onCreateNameChange={onNameChange}
      onCreateDescriptionChange={onDescChange}
      onCreateSubmit={onSubmit}
      onAddCardClick={onOpenCreate}
      emptyState={
        hasNoProjects && onOpenCreate
          ? {
              line1: 'No hay proyectos todavía.',
              line2: 'Crea tu primer proyecto.',
              onOpenCreate,
              ariaLabel: 'Crear primer proyecto',
            }
          : undefined
      }
    />
  )
}
