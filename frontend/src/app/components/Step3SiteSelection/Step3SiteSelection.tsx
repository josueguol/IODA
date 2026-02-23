import type { ReactNode } from 'react'
import { LoadingSpinner } from '../../../shared/components'
import { SiteCardsSection } from './SiteCardsSection'
import '../OnboardingStep.css'

export interface SiteItem {
  id: string
  name: string
  domain: string
  subdomain?: string | null
  subpath?: string | null
  isActive?: boolean
}

export interface Step3SiteSelectionProps {
  sites: SiteItem[]
  loading: boolean
  showCreate: boolean
  currentSiteId: string | null
  newSiteName: string
  newSiteDomain: string
  newSiteSubdomain: string
  newSiteSubpath: string
  newSiteThemeId: string
  newSiteUrlTemplate: string
  createError: string | null
  saving: boolean
  onSelectSite: (id: string) => void
  onDeselectSite: () => void
  onNameChange: (value: string) => void
  onDomainChange: (value: string) => void
  onSubdomainChange: (value: string) => void
  onSubpathChange: (value: string) => void
  onThemeIdChange: (value: string) => void
  onUrlTemplateChange: (value: string) => void
  onSubmit: () => void
  actions: ReactNode
  onOpenCreate?: () => void
}

export function Step3SiteSelection({
  sites,
  loading,
  showCreate,
  currentSiteId,
  newSiteName,
  newSiteDomain,
  newSiteSubdomain,
  newSiteSubpath,
  newSiteThemeId,
  newSiteUrlTemplate,
  createError,
  saving,
  onSelectSite,
  onDeselectSite,
  onNameChange,
  onDomainChange,
  onSubdomainChange,
  onSubpathChange,
  onThemeIdChange,
  onUrlTemplateChange,
  onSubmit,
  actions,
  onOpenCreate,
}: Step3SiteSelectionProps) {
  const title = 'Sitios'

  if (loading && sites.length === 0) {
    return (
      <section className="onboarding-step onboarding-step--clean" aria-labelledby="step3-title">
        <h2 id="step3-title" className="onboarding-step__title">
          {title}
        </h2>
        <LoadingSpinner text="Cargando sitios…" />
      </section>
    )
  }

  return (
    <SiteCardsSection
      sectionId="step3"
      title={title}
      sites={sites}
      currentSiteId={currentSiteId}
      onSelectSite={onSelectSite}
      onDeselectSite={onDeselectSite}
      actions={actions}
      showCreate={showCreate}
      createName={newSiteName}
      createDomain={newSiteDomain}
      createSubdomain={newSiteSubdomain}
      createSubpath={newSiteSubpath}
      createThemeId={newSiteThemeId}
      createUrlTemplate={newSiteUrlTemplate}
      createError={createError}
      createSaving={saving}
      onCreateNameChange={onNameChange}
      onCreateDomainChange={onDomainChange}
      onCreateSubdomainChange={onSubdomainChange}
      onCreateSubpathChange={onSubpathChange}
      onCreateThemeIdChange={onThemeIdChange}
      onCreateUrlTemplateChange={onUrlTemplateChange}
      onCreateSubmit={onSubmit}
      onAddCardClick={onOpenCreate}
      emptyState={
        !loading && sites.length === 0 && onOpenCreate
          ? {
              line1: 'No hay sitios todavía.',
              line2: 'Crea tu primer sitio.',
              onOpenCreate,
              ariaLabel: 'Crear sitio',
            }
          : undefined
      }
    />
  )
}
