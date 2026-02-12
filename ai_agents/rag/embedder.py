from __future__ import annotations

import math
import re
from dataclasses import dataclass


_TOKEN_RE = re.compile(r"[A-Za-zÁÉÍÓÚÜÑáéíóúüñ0-9_]+", re.UNICODE)


@dataclass(frozen=True)
class EmbeddingConfig:
    dim: int = 768


def _stable_hash(token: str) -> int:
    """
    Hash estable (sin depender de hash() aleatorio de Python).
    FNV-1a 32-bit.
    """
    h = 2166136261
    for ch in token:
        h ^= ord(ch)
        h = (h * 16777619) & 0xFFFFFFFF
    return h


def embed_text(text: str, *, cfg: EmbeddingConfig) -> list[float]:
    """
    Embedding determinista "tipo hashing trick" (sin dependencias externas).
    No es SOTA, pero cumple el rol de vector DB local y es suficiente para RAG interno.
    """
    dim = int(cfg.dim)
    if dim <= 0:
        raise ValueError("dim debe ser > 0")

    vec = [0.0] * dim
    tokens = _TOKEN_RE.findall(text.lower())
    if not tokens:
        return vec

    for t in tokens:
        hv = _stable_hash(t)
        idx = hv % dim
        sign = 1.0 if ((hv >> 31) & 1) == 0 else -1.0
        vec[idx] += sign

    # normalización L2
    norm = math.sqrt(sum(v * v for v in vec)) or 1.0
    return [v / norm for v in vec]


def cosine(a: list[float], b: list[float]) -> float:
    if len(a) != len(b) or not a:
        return 0.0
    return sum(x * y for x, y in zip(a, b))

