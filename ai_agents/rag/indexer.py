from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path

from ai_agents.config import AgentConfig
from ai_agents.rag.chunker import chunk_text
from ai_agents.rag.embedder import EmbeddingConfig, embed_text
from ai_agents.rag.vector_store import StoredChunk, VectorStore


@dataclass(frozen=True)
class IndexedFile:
    path: Path
    mtime: float


class RagIndexer:
    def __init__(self, *, cfg: AgentConfig, sqlite_path: Path):
        self.cfg = cfg
        self.store = VectorStore(sqlite_path)
        self.emb_cfg = EmbeddingConfig(dim=self.cfg.embedding_dim)

    def ensure_schema(self) -> None:
        self.store.ensure_schema()

    def _is_excluded(self, p: Path) -> bool:
        parts = set(p.parts)
        return any(ex in parts for ex in self.cfg.exclude_dirs)

    def _should_index_file(self, p: Path) -> bool:
        if self._is_excluded(p):
            return False
        if not p.is_file():
            return False
        if p.suffix.lower() not in self.cfg.include_extensions:
            return False
        try:
            if p.stat().st_size > self.cfg.max_file_bytes:
                return False
        except OSError:
            return False
        return True

    def _iter_files(self) -> list[IndexedFile]:
        out: list[IndexedFile] = []
        for root in self.cfg.index_roots:
            if not root.exists():
                continue
            for dirpath, dirnames, filenames in os.walk(root):
                # prune dirs
                dirnames[:] = [d for d in dirnames if d not in self.cfg.exclude_dirs]
                for fn in filenames:
                    p = Path(dirpath) / fn
                    if not self._should_index_file(p):
                        continue
                    try:
                        out.append(IndexedFile(path=p, mtime=p.stat().st_mtime))
                    except OSError:
                        continue
        return out

    def index_workspace(self) -> None:
        files = self._iter_files()
        for f in files:
            rel_path = str(f.path.relative_to(self.cfg.workspace_root))
            if not self.store.doc_needs_reindex(rel_path, f.mtime):
                continue
            try:
                text = f.path.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue

            chunks_meta = chunk_text(text, chunk_chars=self.cfg.chunk_chars, overlap=self.cfg.chunk_overlap)
            stored: list[StoredChunk] = []
            for start, end, chunk in chunks_meta:
                emb = embed_text(chunk, cfg=self.emb_cfg)
                stored.append(
                    StoredChunk(
                        doc_path=rel_path,
                        doc_mtime=f.mtime,
                        start=start,
                        end=end,
                        text=chunk,
                        embedding=emb,
                    )
                )
            self.store.upsert_doc(rel_path, f.mtime, stored)

