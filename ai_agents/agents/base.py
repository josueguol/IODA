from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class AgentContext:
    question: str
    rag_context: str
    rag_citations: list[dict]
    memory_context: list[dict]


@dataclass(frozen=True)
class AgentResult:
    agent_name: str
    findings: str
    actions: str


class BaseAgent:
    name: str

    def run(self, ctx: AgentContext) -> AgentResult:
        raise NotImplementedError

