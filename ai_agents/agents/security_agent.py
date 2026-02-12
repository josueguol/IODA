from __future__ import annotations

from ai_agents.agents.base import AgentContext, AgentResult, BaseAgent


class SecurityAgent(BaseAgent):
    name = "seguridad"

    def run(self, ctx: AgentContext) -> AgentResult:
        findings = []
        actions = []

        text = (ctx.rag_context or "").lower()

        if "allowanyorigin" in text:
            findings.append("- CORS aparece como `AllowAnyOrigin()` (riesgo en producción).")
            actions.append("- Restringir CORS por environment: lista de orígenes del frontend, sin `AllowAnyOrigin` en producción.")

        if "[authorize]" in text or "authorize" in ctx.question.lower():
            findings.append("- Hay preocupación por autorización/autenticación en endpoints (según diagnóstico, crítico en Authorization/Publishing/Indexing).")
            actions.append("- Auditar controllers de Authorization/Publishing/Indexing y aplicar `[Authorize]` + políticas/roles; documentar matriz rol→endpoint.")

        if "jwt" in text or "secretkey" in text:
            findings.append("- Configuración JWT puede dejar servicios sin auth si falta `Jwt:SecretKey` (según diagnóstico).")
            actions.append("- Hacer fail-fast en startup si `Jwt:SecretKey` falta en no-Development; mover secretos a variables de entorno/vault.")

        if not findings:
            findings.append("- No se detectaron señales fuertes en el contexto recuperado; el diagnóstico igual marca seguridad como Fase 1.")
            actions.append("- Recuperar `Program.cs` de servicios + controllers expuestos para confirmar `[Authorize]` y CORS.")

        return AgentResult(
            agent_name=self.name,
            findings="\n".join(findings),
            actions="\n".join(actions),
        )

