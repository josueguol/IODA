import type { ReactNode } from 'react'
import { OnboardingCard, getInitials } from '../../OnboardingCard'
import { SiteCreateForm } from '../SiteCreateForm'
import { ArrowLeft } from 'lucide-react'
import '../../OnboardingStep.css'

export interface SiteItem {
  id: string
  name: string
  domain: string
  subdomain?: string | null
  subpath?: string | null
  isActive?: boolean
}

export interface SiteCardsSectionEmptyState {
  line1: string
  line2: string
  onOpenCreate: () => void
  ariaLabel?: string
}

export interface SiteCardsSectionProps {
  title: string
  sites: SiteItem[]
  currentSiteId: string | null
  onSelectSite: (id: string) => void
  /** Tarjeta/botón Crear sitio (se muestra en la fila de tarjetas o en estado vacío) */
  actions: ReactNode
  /** Botón opcional para deseleccionar sitio cuando hay uno seleccionado */
  onDeselectSite?: () => void
  showCreate: boolean
  createName: string
  createDomain: string
  createSubdomain: string
  createSubpath: string
  createThemeId: string
  createUrlTemplate: string
  createError: string | null
  createSaving: boolean
  onCreateNameChange: (value: string) => void
  onCreateDomainChange: (value: string) => void
  onCreateSubdomainChange: (value: string) => void
  onCreateSubpathChange: (value: string) => void
  onCreateThemeIdChange: (value: string) => void
  onCreateUrlTemplateChange: (value: string) => void
  onCreateSubmit: () => void
  sectionId?: string
  emptyState?: SiteCardsSectionEmptyState
  onAddCardClick?: () => void
}

export function SiteCardsSection({
  title,
  sites,
  currentSiteId,
  onSelectSite,
  actions,
  onDeselectSite,
  showCreate,
  createName,
  createDomain,
  createSubdomain,
  createSubpath,
  createThemeId,
  createUrlTemplate,
  createError,
  createSaving,
  onCreateNameChange,
  onCreateDomainChange,
  onCreateSubdomainChange,
  onCreateSubpathChange,
  onCreateThemeIdChange,
  onCreateUrlTemplateChange,
  onCreateSubmit,
  sectionId = 'step3',
  emptyState,
  onAddCardClick,
}: SiteCardsSectionProps) {
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
          {sites.length > 0 && (
            <>
              {sites.map((s) => (
                <OnboardingCard
                  key={s.id}
                  initials={getInitials(s.name)}
                  title={s.name}
                  selected={currentSiteId === s.id}
                  onClick={() => onSelectSite(s.id)}
                  id={s.id === currentSiteId ? `${sectionId}-selected-site` : undefined}
                />
              ))}
            </>
          )}
          <div className="work-area__first-action">
            {onDeselectSite && currentSiteId && (
              <button
                type="button"
                className="onboarding-step__submit onboarding-step__submit--secondary"
                onClick={onDeselectSite}
              >
                Deseleccionar sitio
              </button>
            )}
            {onAddCardClick && actions && (
              <div className="onboarding-step__actions">{actions}</div>
            )}
            {emptyState && sites.length === 0 && !showCreate && (
              <>
                <span className="work-area__helper" aria-hidden>
                  <ArrowLeft size={28} strokeWidth={2} className="work-area__helper-arrow" />
                  <span className="work-area__helper-text">{emptyState.line2}</span>
                </span>
              </>
            )}
          </div>
          {emptyState && sites.length === 0 && !showCreate && (
            <p className="work-area__center-message">{emptyState.line1}</p>
          )}
        </div>
      </>

      {showCreate && (
        <SiteCreateForm
          idPrefix={sectionId}
          name={createName}
          domain={createDomain}
          subdomain={createSubdomain}
          subpath={createSubpath}
          themeId={createThemeId}
          urlTemplate={createUrlTemplate}
          error={createError}
          saving={createSaving}
          onNameChange={onCreateNameChange}
          onDomainChange={onCreateDomainChange}
          onSubdomainChange={onCreateSubdomainChange}
          onSubpathChange={onCreateSubpathChange}
          onThemeIdChange={onCreateThemeIdChange}
          onUrlTemplateChange={onCreateUrlTemplateChange}
          onSubmit={onCreateSubmit}
          onCancel={onAddCardClick}
        />
      )}
    </section>
  )
}
