from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

from ai_agents.config import AgentConfig
from ai_agents.rag.embedder import EmbeddingConfig, cosine, embed_text
from ai_agents.rag.vector_store import VectorStore


@dataclass(frozen=True)
class RagHit:
    path: str
    start: int
    end: int
    score: float
    text: str


@dataclass(frozen=True)
class RagResult:
    hits: list[RagHit]
    context_text: str
    citations: list[dict]


class RagRetriever:
    def __init__(self, *, cfg: AgentConfig, sqlite_path: Path):
        self.cfg = cfg
        self.store = VectorStore(sqlite_path)
        self.emb_cfg = EmbeddingConfig(dim=self.cfg.embedding_dim)

    def retrieve(self, query: str) -> RagResult:
        q_emb = embed_text(query, cfg=self.emb_cfg)
        scored: list[RagHit] = []

        for row in self.store.iter_chunks():
            try:
                emb = json.loads(row["embedding_json"])
            except Exception:
                continue
            score = cosine(q_emb, emb)
            if score <= 0:
                continue
            scored.append(
                RagHit(
                    path=row["path"],
                    start=int(row["start"]),
                    end=int(row["end"]),
                    score=float(score),
                    text=row["text"],
                )
            )

        scored.sort(key=lambda h: h.score, reverse=True)
        hits = scored[: self.cfg.top_k]

        context_parts: list[str] = []
        citations: list[dict] = []
        for h in hits:
            context_parts.append(
                f"=== {h.path} [{h.start}:{h.end}] score={h.score:.3f} ===\n{h.text}"
            )
            citations.append({"path": h.path, "start": h.start, "end": h.end, "score": h.score})

        return RagResult(
            hits=hits,
            context_text="\n\n".join(context_parts) if context_parts else "(sin contexto recuperado)",
            citations=citations,
        )

