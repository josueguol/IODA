from __future__ import annotations

from ai_agents.agents.base import AgentContext, AgentResult, BaseAgent


class DevOpsAgent(BaseAgent):
    name = "devops"

    def run(self, ctx: AgentContext) -> AgentResult:
        findings = [
            "- El diagnóstico menciona duplicación de configuración (JWT/CORS/Swagger), secretos en `appsettings.json` y `docker-compose.yml`, y ausencia de `depends_on`/health checks.",
        ]
        actions = [
            "- Mover secretos a variables de entorno/secret store; dejar `appsettings.Development.json` con valores locales no sensibles.",
            "- Validación al arranque (Options + ValidateOnStart) para `ConnectionStrings` y `Jwt:SecretKey` en no-Development.",
            "- Añadir health checks y `depends_on` con condiciones cuando Postgres/RabbitMQ estén en el mismo compose.",
            "- Estandarizar extensiones compartidas para JWT/CORS/Swagger.",
        ]
        return AgentResult(agent_name=self.name, findings="\n".join(findings), actions="\n".join(actions))

