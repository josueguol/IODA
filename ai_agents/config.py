from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class AgentConfig:
    # Raíz del workspace
    workspace_root: Path

    # Base de datos (vector store + memoria) local
    state_dir: Path
    sqlite_path: Path

    # RAG: qué indexar
    index_roots: tuple[Path, ...]
    include_extensions: tuple[str, ...]
    exclude_dirs: tuple[str, ...]
    max_file_bytes: int
    chunk_chars: int
    chunk_overlap: int
    embedding_dim: int

    # LLM: provider "ollama" | "openai" | "anthropic"
    llm_provider: str
    ollama_url: str
    ollama_model: str
    openai_model: str
    anthropic_model: str
    temperature: float
    # API key (o usar env OPENAI_API_KEY / ANTHROPIC_API_KEY)
    openai_api_key: str | None
    anthropic_api_key: str | None

    # Recuperación
    top_k: int


def default_config(workspace_root: str | Path) -> AgentConfig:
    root = Path(workspace_root).resolve()
    state_dir = root / ".ai_state"
    sqlite_path = state_dir / "ai_agents.sqlite3"

    # Importante: evitar obj/, bin/, artifacts/, node_modules/ para que el índice no se contamine.
    exclude_dirs = (
        ".git",
        ".ai_state",
        "bin",
        "obj",
        "artifacts",
        "node_modules",
        "dist",
        "build",
        ".idea",
        ".vscode",
    )

    return AgentConfig(
        workspace_root=root,
        state_dir=state_dir,
        sqlite_path=sqlite_path,
        index_roots=(
            root / "docs",
            root / "src",
            root / "frontend",
        ),
        include_extensions=(
            ".md",
            ".txt",
            ".cs",
            ".csproj",
            ".sln",
            ".json",
            ".yml",
            ".yaml",
            ".ts",
            ".tsx",
            ".js",
            ".jsx",
        ),
        exclude_dirs=exclude_dirs,
        max_file_bytes=600_000,  # evita archivos muy grandes
        chunk_chars=1800,
        chunk_overlap=250,
        embedding_dim=768,
        llm_provider=os.getenv("LLM_PROVIDER", "ollama").lower(),
        ollama_url=os.getenv("OLLAMA_URL", "http://localhost:11434/api/generate"),
        ollama_model=os.getenv("OLLAMA_MODEL", "gemma3:4b"),
        openai_model=os.getenv("OPENAI_MODEL", "gpt-4o-mini"),
        anthropic_model=os.getenv("ANTHROPIC_MODEL", "claude-sonnet-4-20250514"),
        temperature=float(os.getenv("LLM_TEMPERATURE", "0.2")),
        openai_api_key=os.getenv("OPENAI_API_KEY") or None,
        anthropic_api_key=os.getenv("ANTHROPIC_API_KEY") or None,
        top_k=10,
    )