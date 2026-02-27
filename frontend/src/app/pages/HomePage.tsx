import { useEffect } from 'react'
import { Can } from '../../modules/authorization/components/Can'
import { useContextStore } from '../../modules/core/store/context-store'
import { ErrorBanner } from '../../shared/components'
import { HomeDashboard } from '../components/HomeDashboard'
import { HomeProjectsError } from '../components/HomeProjectsError'
import { Step1ProjectCreation } from '../components/Step1ProjectCreation'
import { Step2EnvironmentSelection } from '../components/Step2EnvironmentSelection'
import { Step3SiteSelection } from '../components/Step3SiteSelection'
import { AddCard } from '../components/OnboardingCard'
import { useCreateProject, useCreateEnvironment, useCreateSite } from '../hooks'
import '../components/OnboardingStep.css'
import './HomePage.css'

export function HomePage() {
  const {
    currentProjectId,
    currentEnvironmentId,
    currentSiteId,
    projects,
    environments,
    sites,
    projectsLoading,
    environmentsLoading,
    sitesLoading,
    projectsError,
    environmentsError,
    sitesError,
    setProject,
    setEnvironment,
    setSite,
    loadProjects,
    loadEnvironments,
    loadSites,
  } = useContextStore()

  const createProject = useCreateProject({ loadProjects, setProject })
  const createEnv = useCreateEnvironment({
    currentProjectId,
    loadEnvironments,
    setEnvironment,
  })
  const createSite = useCreateSite({
    currentProjectId,
    currentEnvironmentId,
    loadSites,
    setSite,
  })

  useEffect(() => {
    loadProjects().catch(() => {})
  }, [loadProjects])

  useEffect(() => {
    if (currentProjectId) {
      loadEnvironments(currentProjectId).catch(() => {})
    }
  }, [currentProjectId, loadEnvironments])

  useEffect(() => {
    if (currentProjectId && currentEnvironmentId) {
      loadSites(currentProjectId, currentEnvironmentId).catch(() => {})
    }
  }, [currentProjectId, currentEnvironmentId, loadSites])

  const hasNoProjects = !projectsLoading && projects.length === 0
  const hasFullContext = Boolean(currentProjectId && currentEnvironmentId && currentSiteId)
  const contextState: 'EMPTY' | 'PROJECT_SELECTED' | 'PROJECT_ENV_SELECTED' | 'FULL_CONTEXT' =
    !currentProjectId
      ? 'EMPTY'
      : !currentEnvironmentId
        ? 'PROJECT_SELECTED'
        : !currentSiteId
          ? 'PROJECT_ENV_SELECTED'
          : 'FULL_CONTEXT'

  const createProjectActions = (
    <Can permission="project.create" fallback={null}>
      <AddCard onClick={createProject.toggleCreate} ariaLabel="Crear proyecto" legend="Crear proyecto" />
    </Can>
  )

  return (
    <>
      {projectsError && <HomeProjectsError message={projectsError} />}
      {environmentsError && <ErrorBanner message={environmentsError} />}

      {contextState === 'EMPTY' && (
        <Step1ProjectCreation
          projects={projects}
          hasNoProjects={hasNoProjects}
          loading={projectsLoading}
          showCreate={createProject.showCreate}
          currentProjectId={currentProjectId}
          newProjectName={createProject.name}
          newProjectDesc={createProject.description}
          createError={createProject.error}
          saving={createProject.saving}
          onSelectProject={setProject}
          onNameChange={createProject.setName}
          onDescChange={createProject.setDescription}
          onSubmit={createProject.createProject}
          onOpenCreate={createProject.toggleCreate}
          actions={createProjectActions}
        />
      )}

      {contextState === 'PROJECT_SELECTED' && (
        <Step2EnvironmentSelection
          environments={environments}
          loading={environmentsLoading}
          showCreate={createEnv.showCreate}
          currentEnvironmentId={currentEnvironmentId}
          newEnvName={createEnv.name}
          newEnvDesc={createEnv.description}
          createError={createEnv.error}
          saving={createEnv.saving}
          onSelectEnvironment={setEnvironment}
          onNameChange={createEnv.setName}
          onDescChange={createEnv.setDescription}
          onSubmit={createEnv.createEnvironment}
          onOpenCreate={createEnv.toggleCreate}
          actions={
            <Can permission="environment.create" fallback={null}>
              <AddCard
                onClick={createEnv.toggleCreate}
                ariaLabel="Crear entorno"
                legend="Crear entorno"
              />
            </Can>
          }
        />
      )}

      {contextState === 'PROJECT_ENV_SELECTED' && (
        <>
          {sitesError && <ErrorBanner message={sitesError} />}
          <Step3SiteSelection
            sites={sites.map((s) => ({
              id: s.id,
              name: s.name,
              domain: s.domain,
              subdomain: s.subdomain ?? null,
              subpath: s.subpath ?? null,
              isActive: s.isActive,
            }))}
            loading={sitesLoading}
            showCreate={createSite.showCreate}
            currentSiteId={currentSiteId}
            newSiteName={createSite.name}
            newSiteDomain={createSite.domain}
            newSiteSubdomain={createSite.subdomain}
            newSiteSubpath={createSite.subpath}
            newSiteThemeId={createSite.themeId}
            newSiteUrlTemplate={createSite.urlTemplate}
            createError={createSite.error}
            saving={createSite.saving}
            onSelectSite={setSite}
            onDeselectSite={() => setSite(null)}
            onNameChange={createSite.setName}
            onDomainChange={createSite.setDomain}
            onSubdomainChange={createSite.setSubdomain}
            onSubpathChange={createSite.setSubpath}
            onThemeIdChange={createSite.setThemeId}
            onUrlTemplateChange={createSite.setUrlTemplate}
            onSubmit={createSite.createSite}
            onOpenCreate={createSite.toggleCreate}
            actions={
              <Can permission="site.create" fallback={null}>
                <AddCard
                  onClick={createSite.toggleCreate}
                  ariaLabel="Crear sitio"
                  legend="Crear sitio"
                />
              </Can>
            }
          />
        </>
      )}

      {hasFullContext && (
        <HomeDashboard />
      )}
    </>
  )
}
