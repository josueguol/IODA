import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { useContextStore } from '../../modules/core/store/context-store'
import { useSchemaStore } from '../../modules/schema/store/schema-store'
import { AddCard, OnboardingCard, getInitials } from '../components/OnboardingCard'
import '../components/OnboardingStep.css'
import './SchemaListPage.css'

export function SchemaListPage() {
  const navigate = useNavigate()
  const { currentProjectId } = useContextStore()
  const { schemaList, listLoading, listError, loadSchemas } = useSchemaStore()

  useEffect(() => {
    if (!currentProjectId) return
    loadSchemas(currentProjectId).catch(() => {})
  }, [currentProjectId, loadSchemas])

  if (!currentProjectId) {
    return (
      <section className="schema-list-page">
        <h1 className="schema-list-page__title">Schemas</h1>
        <p className="schema-list-page__hint">Selecciona un proyecto para listar y diseñar schemas.</p>
      </section>
    )
  }

  return (
    <section className="schema-list-page">
      <h1 className="schema-list-page__title">Schemas</h1>
      <p className="schema-list-page__hint">
        Administra tus tipos de contenido. Selecciona un schema para editarlo o crea uno nuevo.
      </p>

      {listError && <p className="schema-list-page__error">{listError}</p>}
      {listLoading && <p className="schema-list-page__hint">Cargando schemas…</p>}

      {!listLoading && (
        <div className="work-area work-area--empty">
          {schemaList.map((schema) => (
            <OnboardingCard
              key={schema.id}
              initials={getInitials(schema.schemaName)}
              title={schema.schemaName}
              onClick={() => navigate(`/admin/schemas/design?schemaId=${encodeURIComponent(schema.id)}`)}
            />
          ))}
          <div className="work-area__first-action">
            <AddCard
              onClick={() => navigate('/admin/schemas/design')}
              ariaLabel="Crear schema"
              legend="Crear schema"
            />
            {schemaList.length === 0 && (
              <span className="work-area__helper" aria-hidden>
                <ArrowLeft size={28} strokeWidth={2} className="work-area__helper-arrow" />
                <span className="work-area__helper-text">Crea tu primer esquema.</span>
              </span>
            )}
          </div>
          {schemaList.length === 0 && (
            <p className="work-area__center-message">No hay esquemas todavía.</p>
          )}
        </div>
      )}
    </section>
  )
}
