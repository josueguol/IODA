from __future__ import annotations

from ai_agents.agents.base import AgentContext, AgentResult, BaseAgent


class ArchitectureAgent(BaseAgent):
    name = "arquitectura"

    def run(self, ctx: AgentContext) -> AgentResult:
        findings = [
            "- El diagnóstico técnico indica violaciones de capas (ej. controller dependiendo de repositorio del dominio) y contratos HTTP filtrando tipos de dominio (enums)."
        ]
        actions = [
            "- En Identity: mover lógica de `AuthController` a Application (query/handler tipo `GetSetupStatusQuery`/`IsFirstUserQuery`) y que API solo use MediatR/DTOs.",
            "- En Publishing: evitar exponer `PublicationRequestStatus` del dominio en query params; usar string/DTO y mapear en Application.",
            "- Centralizar `ErrorHandlingMiddleware` en un paquete compartido y mapear excepciones por convención/registro (evitar duplicación por servicio).",
            "- Estandarizar excepciones “not found”: reemplazar `InvalidOperationException` por excepciones de dominio (ej. `SchemaNotFoundException`) para HTTP 404 consistente.",
        ]

        # Si el contexto RAG trae rutas específicas, reforzamos acciones.
        rag = (ctx.rag_context or "")
        if "AuthController.cs" in rag:
            findings.append("- El contexto recuperado menciona `AuthController.cs` como foco de lógica de negocio en API.")
        if "ErrorHandlingMiddleware" in rag:
            findings.append("- El middleware de errores aparece duplicado entre servicios.")

        return AgentResult(
            agent_name=self.name,
            findings="\n".join(findings),
            actions="\n".join(actions),
        )

