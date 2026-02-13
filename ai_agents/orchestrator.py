from __future__ import annotations

from dataclasses import dataclass

from ai_agents.config import AgentConfig
from ai_agents.llm_client import LlmConfig, llm_generate
from ai_agents.memory.adaptive_memory import AdaptiveMemory
from ai_agents.rag.indexer import RagIndexer
from ai_agents.rag.retriever import RagRetriever
from ai_agents.agents.architecture_agent import ArchitectureAgent
from ai_agents.agents.base import AgentContext, AgentResult, BaseAgent
from ai_agents.agents.devops_agent import DevOpsAgent
from ai_agents.agents.frontend_agent import FrontendAgent
from ai_agents.agents.security_agent import SecurityAgent
from ai_agents.agents.testing_agent import TestingAgent


@dataclass(frozen=True)
class OrchestratorAnswer:
    answer: str
    selected_agents: list[str]
    citations: list[dict]
    memory_notes: list[str]


class Orchestrator:
    """
    Orquestador de agentes para análisis del proyecto con RAG + memoria adaptativa.

    Flujo:
    - Indexación (incremental) de docs/código a un vector store local (SQLite).
    - Recuperación de contexto relevante.
    - Selección de agentes especializados (seguridad, arquitectura, tests, devops, frontend).
    - Respuesta del LLM basada en el contexto + memoria.
    - Persistencia de memoria adaptativa (decisiones, riesgos, plan).
    """

    def __init__(self, cfg: AgentConfig):
        self.cfg = cfg
        self.cfg.state_dir.mkdir(parents=True, exist_ok=True)

        self.memory = AdaptiveMemory(sqlite_path=self.cfg.sqlite_path)
        self.indexer = RagIndexer(cfg=self.cfg, sqlite_path=self.cfg.sqlite_path)
        self.retriever = RagRetriever(cfg=self.cfg, sqlite_path=self.cfg.sqlite_path)

        self.agents: list[BaseAgent] = [
            SecurityAgent(),
            ArchitectureAgent(),
            TestingAgent(),
            DevOpsAgent(),
            FrontendAgent(),
        ]

        provider = (self.cfg.llm_provider or "ollama").lower()
        if provider == "openai":
            model = self.cfg.openai_model
        elif provider == "anthropic":
            model = self.cfg.anthropic_model
        else:
            model = self.cfg.ollama_model
        self.llm_cfg = LlmConfig(
            provider=provider,
            model=model,
            temperature=self.cfg.temperature,
            timeout_seconds=self.cfg.llm_timeout_seconds,
            ollama_url=self.cfg.ollama_url,
            openai_api_key=self.cfg.openai_api_key,
            anthropic_api_key=self.cfg.anthropic_api_key,
        )

    def ensure_index(self) -> None:
        self.indexer.ensure_schema()
        self.indexer.index_workspace()

    def _select_agents(self, question: str) -> list[BaseAgent]:
        q = question.lower()
        selected: list[BaseAgent] = []

        # Heurística simple y transparente (se puede mejorar a futuro con clasificación).
        def want(*terms: str) -> bool:
            return any(t in q for t in terms)

        for agent in self.agents:
            if agent.name == "seguridad" and want("authorize", "[authorize]", "jwt", "cors", "secreto", "secret", "auth", "autoriz"):
                selected.append(agent)
            elif agent.name == "arquitectura" and want("clean", "capas", "controller", "repositorio", "ddd", "mediatr", "dto", "middleware"):
                selected.append(agent)
            elif agent.name == "testing" and want("test", "pruebas", "xunit", "integration", "unit", "coverage", "testcontainers"):
                selected.append(agent)
            elif agent.name == "devops" and want("docker", "compose", "ci", "pipeline", "deploy", "health", "env", "secrets"):
                selected.append(agent)
            elif agent.name == "frontend" and want("frontend", "vite", "react", "tsx", "ui", "puertos", "cors"):
                selected.append(agent)

        # Si no se detecta nada, usamos un set base centrado en lo crítico del diagnóstico.
        if not selected:
            selected = [SecurityAgent(), TestingAgent(), ArchitectureAgent()]

        # Deduplicación por name
        uniq: dict[str, BaseAgent] = {a.name: a for a in selected}
        return list(uniq.values())

    def answer(self, question: str) -> OrchestratorAnswer:
        self.ensure_index()

        rag = self.retriever.retrieve(question)
        memories = self.memory.retrieve_relevant(question, k=6)

        selected = self._select_agents(question)

        ctx = AgentContext(
            question=question,
            rag_context=rag.context_text,
            rag_citations=rag.citations,
            memory_context=memories,
        )

        results: list[AgentResult] = []
        for agent in selected:
            results.append(agent.run(ctx))

        synthesis_prompt = _build_synthesis_prompt(
            question=question,
            rag_context=rag.context_text,
            memory_items=memories,
            agent_results=results,
        )

        raw_answer = llm_generate(synthesis_prompt, cfg=self.llm_cfg).strip()
        final_answer = _ensure_spanish_and_format(raw_answer, question=question, llm_cfg=self.llm_cfg).strip()

        # Memoria adaptativa: guardamos resumen/decisiones extraídas de la respuesta.
        memory_notes = self.memory.update_from_interaction(
            question=question,
            answer=final_answer,
            agent_results=results,
            citations=rag.citations,
        )

        return OrchestratorAnswer(
            answer=final_answer,
            selected_agents=[a.name for a in selected],
            citations=rag.citations,
            memory_notes=memory_notes,
        )


def _build_synthesis_prompt(
    *,
    question: str,
    rag_context: str,
    memory_items: list[dict],
    agent_results: list[AgentResult],
) -> str:
    memory_block = "\n".join(
        f"- ({m.get('kind')}) {m.get('text')}" for m in memory_items
    ) or "(sin memoria relevante)"

    agent_block = "\n\n".join(
        f"[Agente: {r.agent_name}]\nHallazgos:\n{r.findings}\n\nAcciones sugeridas:\n{r.actions}"
        for r in agent_results
    )

    return f"""
Eres un arquitecto senior de software. Responde ÚNICAMENTE en español.
Si el usuario pide algo en español, jamás respondas en inglés.

OBJETIVO
- Contestar la pregunta del usuario usando SOLO el contexto recuperado (RAG) + memoria + hallazgos de agentes.
- Si faltan datos, dilo explícitamente y sugiere dónde buscar dentro del repo.
- Devuelve una respuesta accionable y priorizada (alineada al diagnóstico técnico del CMS).

PREGUNTA
{question}

MEMORIA (decisiones/historial relevante)
{memory_block}

CONTEXTO RAG (fragmentos de repo: docs/código/config)
{rag_context}

HALLAZGOS DE AGENTES
{agent_block}

FORMATO DE SALIDA
- Resumen corto (2-4 líneas)
- Hallazgos clave (bullets)
- Plan priorizado
  - Fase 1 (crítico)
  - Fase 2 (estructural)
  - Fase 3 (optimización)
- Riesgos / dependencias
""".strip()


def _ensure_spanish_and_format(text: str, *, question: str, llm_cfg: LlmConfig) -> str:
    """
    Algunos modelos pueden responder en inglés aunque se pida español.
    Hacemos un segundo pase para traducir y normalizar formato si detectamos inglés.
    """
    sample = text[:500].lower()
    looks_english = any(w in sample for w in (" let's ", " here's ", "overall", "action items", "do you want me"))
    if not looks_english:
        return text

    rewrite_prompt = f"""
Reescribe el siguiente contenido en español neutro, manteniendo el significado.
No agregues información nueva. No hagas preguntas al final.
Aplica el formato:
- Resumen corto
- Hallazgos clave
- Plan priorizado (Fase 1, Fase 2, Fase 3)
- Riesgos / dependencias

Pregunta original:
{question}

Contenido a reescribir:
{text}
""".strip()
    return llm_generate(rewrite_prompt, cfg=llm_cfg).strip()
