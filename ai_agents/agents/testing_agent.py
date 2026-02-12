from __future__ import annotations

from ai_agents.agents.base import AgentContext, AgentResult, BaseAgent


class TestingAgent(BaseAgent):
    name = "testing"

    def run(self, ctx: AgentContext) -> AgentResult:
        findings = [
            "- El diagnóstico indica cobertura de tests ~0% y ausencia de proyectos `*Tests`."
        ]
        actions = [
            "- Crear proyectos de tests (unit + integration) al menos para Core e Identity.",
            "- Priorizar casos: Create/Update Content, Publish/Unpublish, ApprovePublication, Login/Register, CheckAccess, SchemaValidationService.",
            "- Usar TestContainers para PostgreSQL (y opcional RabbitMQ/Elasticsearch) en integration tests.",
            "- Configurar cobertura en CI (coverlet) y umbral mínimo gradual.",
        ]

        return AgentResult(agent_name=self.name, findings="\n".join(findings), actions="\n".join(actions))

