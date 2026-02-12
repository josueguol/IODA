from __future__ import annotations

from ai_agents.agents.base import AgentContext, AgentResult, BaseAgent


class FrontendAgent(BaseAgent):
    name = "frontend"

    def run(self, ctx: AgentContext) -> AgentResult:
        findings = []
        actions = []
        rag = (ctx.rag_context or "").lower()

        if "import.meta.env" in rag or "vite" in rag:
            findings.append("- El frontend usa Vite (`import.meta.env`) con fallbacks a localhost.")
            actions.append("- Alinear configuración de puertos/URLs entre dev (sin Docker) y docker-compose; documentar en `docs/`.")

        if "cors" in rag:
            findings.append("- CORS impacta directamente al frontend (diagnóstico: demasiado permisivo).")
            actions.append("- Definir lista de orígenes del frontend por environment y probar flujos login/refresh + llamadas cross-origin.")

        if not findings:
            findings.append("- No hubo señales frontend fuertes en el contexto recuperado; el diagnóstico sí menciona mismatch de puertos.")
            actions.append("- Revisar `frontend/config/env.ts` y `docker-compose.yml` para unificar endpoints.")

        return AgentResult(agent_name=self.name, findings="\n".join(findings), actions="\n".join(actions))

