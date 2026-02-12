from __future__ import annotations

import json
import sqlite3
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


@dataclass(frozen=True)
class StoredChunk:
    doc_path: str
    doc_mtime: float
    start: int
    end: int
    text: str
    embedding: list[float]


class VectorStore:
    """
    Vector DB local en SQLite (embeddings guardados como JSON).
    - Tabla docs: documentos vistos + mtime
    - Tabla chunks: chunks con embedding y metadata
    """

    def __init__(self, sqlite_path: Path):
        self.sqlite_path = Path(sqlite_path)
        self._conn = sqlite3.connect(self.sqlite_path)
        self._conn.row_factory = sqlite3.Row

    def close(self) -> None:
        try:
            self._conn.close()
        except Exception:
            pass

    def ensure_schema(self) -> None:
        cur = self._conn.cursor()
        cur.execute(
            """
            CREATE TABLE IF NOT EXISTS rag_docs (
              path TEXT PRIMARY KEY,
              mtime REAL NOT NULL
            );
            """
        )
        cur.execute(
            """
            CREATE TABLE IF NOT EXISTS rag_chunks (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              path TEXT NOT NULL,
              mtime REAL NOT NULL,
              start INTEGER NOT NULL,
              end INTEGER NOT NULL,
              text TEXT NOT NULL,
              embedding_json TEXT NOT NULL
            );
            """
        )
        cur.execute("CREATE INDEX IF NOT EXISTS idx_rag_chunks_path ON rag_chunks(path);")
        self._conn.commit()

    def doc_needs_reindex(self, path: str, mtime: float) -> bool:
        cur = self._conn.cursor()
        row = cur.execute("SELECT mtime FROM rag_docs WHERE path = ?", (path,)).fetchone()
        if row is None:
            return True
        return float(row["mtime"]) != float(mtime)

    def delete_doc(self, path: str) -> None:
        cur = self._conn.cursor()
        cur.execute("DELETE FROM rag_chunks WHERE path = ?", (path,))
        cur.execute("DELETE FROM rag_docs WHERE path = ?", (path,))
        self._conn.commit()

    def upsert_doc(self, path: str, mtime: float, chunks: Iterable[StoredChunk]) -> None:
        cur = self._conn.cursor()
        cur.execute("DELETE FROM rag_chunks WHERE path = ?", (path,))
        cur.execute(
            "INSERT INTO rag_docs(path, mtime) VALUES(?, ?) "
            "ON CONFLICT(path) DO UPDATE SET mtime=excluded.mtime",
            (path, float(mtime)),
        )
        cur.executemany(
            """
            INSERT INTO rag_chunks(path, mtime, start, end, text, embedding_json)
            VALUES(?, ?, ?, ?, ?, ?)
            """,
            [
                (
                    c.doc_path,
                    float(c.doc_mtime),
                    int(c.start),
                    int(c.end),
                    c.text,
                    json.dumps(c.embedding, ensure_ascii=False),
                )
                for c in chunks
            ],
        )
        self._conn.commit()

    def iter_chunks(self) -> Iterable[sqlite3.Row]:
        cur = self._conn.cursor()
        for row in cur.execute("SELECT path, start, end, text, embedding_json FROM rag_chunks"):
            yield row

