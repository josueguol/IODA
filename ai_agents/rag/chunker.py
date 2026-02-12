from __future__ import annotations


def chunk_text(text: str, *, chunk_chars: int, overlap: int) -> list[tuple[int, int, str]]:
    """
    Devuelve chunks como (start_char, end_char, chunk_text).
    """
    if chunk_chars <= 0:
        raise ValueError("chunk_chars debe ser > 0")
    if overlap < 0:
        raise ValueError("overlap debe ser >= 0")
    if overlap >= chunk_chars:
        overlap = max(0, chunk_chars // 4)

    chunks: list[tuple[int, int, str]] = []
    n = len(text)
    i = 0
    while i < n:
        j = min(n, i + chunk_chars)
        chunk = text[i:j]
        if chunk.strip():
            chunks.append((i, j, chunk))
        if j >= n:
            break
        i = max(0, j - overlap)
    return chunks

