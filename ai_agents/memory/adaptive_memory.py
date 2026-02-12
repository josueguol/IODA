from __future__ import annotations

import re
import sqlite3
from dataclasses import dataclass
from pathlib import Path


_BULLET_RE = re.compile(r"^\s*[-*]\s+", re.MULTILINE)


@dataclass(frozen=True)
class MemoryItem:
    kind: str  # decision | risk | todo | note
    text: str
    weight: float = 1.0


class AdaptiveMemory:
    """
    Memoria adaptativa simple:
    - Guarda interacciones (Q/A)
    - Extrae bullets/acciones como 'todo' y hallazgos como 'risk/decision' con heurística
    - Recupera por búsqueda LIKE (rápida) + orden por peso
    """

    def __init__(self, *, sqlite_path: Path):
        self.sqlite_path = Path(sqlite_path)
        self._conn = sqlite3.connect(self.sqlite_path)
        self._conn.row_factory = sqlite3.Row
        self.ensure_schema()

    def ensure_schema(self) -> None:
        cur = self._conn.cursor()
        cur.execute(
            """
            CREATE TABLE IF NOT EXISTS mem_items (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              kind TEXT NOT NULL,
              text TEXT NOT NULL,
              weight REAL NOT NULL DEFAULT 1.0
            );
            """
        )
        cur.execute("CREATE INDEX IF NOT EXISTS idx_mem_kind ON mem_items(kind);")
        cur.execute("CREATE INDEX IF NOT EXISTS idx_mem_weight ON mem_items(weight);")

        cur.execute(
            """
            CREATE TABLE IF NOT EXISTS mem_interactions (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              question TEXT NOT NULL,
              answer TEXT NOT NULL,
              created_at TEXT NOT NULL DEFAULT (datetime('now'))
            );
            """
        )
        self._conn.commit()

    def add_item(self, item: MemoryItem) -> None:
        cur = self._conn.cursor()
        cur.execute(
            "INSERT INTO mem_items(kind, text, weight) VALUES(?, ?, ?)",
            (item.kind, item.text.strip(), float(item.weight)),
        )
        self._conn.commit()

    def retrieve_relevant(self, query: str, *, k: int = 6) -> list[dict]:
        """
        Recuperación ligera: busca por tokens (LIKE) y prioriza por weight.
        """
        tokens = [t for t in re.findall(r"[a-zA-ZáéíóúüñÁÉÍÓÚÜÑ0-9_]{3,}", query.lower())][:8]
        if not tokens:
            tokens = [query[:20].lower()]

        where = " OR ".join(["lower(text) LIKE ?"] * len(tokens))
        params = [f"%{t}%" for t in tokens]

        cur = self._conn.cursor()
        rows = cur.execute(
            f"SELECT kind, text, weight FROM mem_items WHERE {where} ORDER BY weight DESC LIMIT ?",
            (*params, int(k)),
        ).fetchall()
        return [{"kind": r["kind"], "text": r["text"], "weight": r["weight"]} for r in rows]

    def update_from_interaction(
        self,
        *,
        question: str,
        answer: str,
        agent_results: list,
        citations: list[dict],
    ) -> list[str]:
        cur = self._conn.cursor()
        cur.execute(
            "INSERT INTO mem_interactions(question, answer) VALUES(?, ?)",
            (question.strip(), answer.strip()),
        )
        self._conn.commit()

        notes: list[str] = []

        # Heurística: bullets → todos; presencia de "crítico" o "riesgo" → risk
        bullets = [b.strip() for b in _BULLET_RE.split(answer) if b.strip()]
        for b in bullets[:12]:
            kind = "todo"
            weight = 1.0
            low = b.lower()
            if "crític" in low or "riesgo" in low or "vulnerab" in low:
                kind = "risk"
                weight = 1.5
            if "decisión" in low or "acord" in low:
                kind = "decision"
                weight = 1.4
            text = b[:400]
            self.add_item(MemoryItem(kind=kind, text=text, weight=weight))
            notes.append(f"{kind}: {text}")

        # Aumenta peso si se citan archivos “core” del diagnóstico
        for c in citations[:10]:
            p = (c.get("path") or "").lower()
            if "diagnostico_tecnico_cms" in p:
                self.add_item(MemoryItem(kind="note", text="Diagnóstico técnico usado como referencia principal.", weight=2.0))
                notes.append("note: Diagnóstico técnico usado como referencia principal.")
                break

        return notes[:12]

