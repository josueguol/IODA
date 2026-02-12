"""
Cliente LLM unificado: Ollama (local), OpenAI o Anthropic.
Usa OPENAI_API_KEY o ANTHROPIC_API_KEY para conectar los mismos proveedores que Cursor.
"""
from __future__ import annotations

import json
from dataclasses import dataclass

import requests


class LlmError(RuntimeError):
    pass


@dataclass(frozen=True)
class LlmConfig:
    provider: str  # "ollama" | "openai" | "anthropic"
    model: str
    temperature: float = 0.2
    # Ollama
    ollama_url: str = "http://localhost:11434/api/generate"
    # API keys (para openai/anthropic)
    openai_api_key: str | None = None
    anthropic_api_key: str | None = None


def llm_generate(prompt: str, *, cfg: LlmConfig) -> str:
    provider = (cfg.provider or "ollama").lower()
    if provider == "openai":
        return _generate_openai(prompt, cfg)
    if provider == "anthropic":
        return _generate_anthropic(prompt, cfg)
    return _generate_ollama(prompt, cfg)


def _generate_ollama(prompt: str, cfg: LlmConfig) -> str:
    try:
        resp = requests.post(
            cfg.ollama_url,
            json={
                "model": cfg.model,
                "prompt": prompt,
                "stream": False,
                "options": {"temperature": cfg.temperature},
            },
            timeout=120,
        )
    except requests.RequestException as e:
        raise LlmError(f"No se pudo conectar al LLM en {cfg.ollama_url}: {e}") from e

    if resp.status_code != 200:
        raise LlmError(f"Error del LLM (status={resp.status_code}): {resp.text[:500]}")

    try:
        data = resp.json()
    except json.JSONDecodeError as e:
        raise LlmError(f"Respuesta inválida del LLM (no JSON): {resp.text[:500]}") from e

    out = data.get("response")
    if not isinstance(out, str):
        raise LlmError("Respuesta del LLM sin campo 'response' string.")
    return out


def _generate_openai(prompt: str, cfg: LlmConfig) -> str:
    api_key = cfg.openai_api_key or ""
    if not api_key.strip():
        raise LlmError(
            "Para usar OpenAI configura OPENAI_API_KEY (env o config). "
            "Puedes usar la misma clave que en Cursor Settings > Models."
        )

    try:
        resp = requests.post(
            "https://api.openai.com/v1/chat/completions",
            headers={
                "Authorization": f"Bearer {api_key}",
                "Content-Type": "application/json",
            },
            json={
                "model": cfg.model,
                "messages": [{"role": "user", "content": prompt}],
                "temperature": cfg.temperature,
                "max_tokens": 4096,
            },
            timeout=120,
        )
    except requests.RequestException as e:
        raise LlmError(f"Error de conexión a OpenAI: {e}") from e

    if resp.status_code != 200:
        raise LlmError(f"OpenAI error (status={resp.status_code}): {resp.text[:500]}")

    try:
        data = resp.json()
    except json.JSONDecodeError as e:
        raise LlmError(f"OpenAI respuesta no JSON: {resp.text[:500]}") from e

    choices = data.get("choices") or []
    if not choices:
        raise LlmError("OpenAI no devolvió choices.")
    msg = choices[0].get("message") or {}
    content = msg.get("content")
    if content is None:
        raise LlmError("OpenAI choice sin message.content.")
    return str(content).strip()


def _generate_anthropic(prompt: str, cfg: LlmConfig) -> str:
    api_key = cfg.anthropic_api_key or ""
    if not api_key.strip():
        raise LlmError(
            "Para usar Anthropic configura ANTHROPIC_API_KEY (env o config). "
            "Puedes usar la misma clave que en Cursor Settings > Models."
        )

    try:
        resp = requests.post(
            "https://api.anthropic.com/v1/messages",
            headers={
                "x-api-key": api_key,
                "anthropic-version": "2023-06-01",
                "Content-Type": "application/json",
            },
            json={
                "model": cfg.model,
                "max_tokens": 4096,
                "messages": [{"role": "user", "content": prompt}],
                "temperature": cfg.temperature,
            },
            timeout=120,
        )
    except requests.RequestException as e:
        raise LlmError(f"Error de conexión a Anthropic: {e}") from e

    if resp.status_code != 200:
        raise LlmError(f"Anthropic error (status={resp.status_code}): {resp.text[:500]}")

    try:
        data = resp.json()
    except json.JSONDecodeError as e:
        raise LlmError(f"Anthropic respuesta no JSON: {resp.text[:500]}") from e

    content = data.get("content") or []
    if not content or not isinstance(content, list):
        raise LlmError("Anthropic no devolvió content.")
    first = content[0]
    if isinstance(first, dict) and first.get("type") == "text":
        return (first.get("text") or "").strip()
    raise LlmError("Anthropic content sin bloque type=text.")
