# Agentes de análisis del CMS (RAG + memoria)

## Ejecución rápida

```bash
python3 -m pip install -r ai_agents/requirements.txt
python3 -m ai_agents.cli "Tu pregunta aquí"
```

## Conectar los modelos que usas en Cursor (OpenAI / Anthropic)

Cursor no expone una API pública para llamar a “sus” modelos desde fuera del IDE. Sí puedes usar **las mismas cuentas** (OpenAI, Anthropic) con sus APIs y tu API key. Así los agentes usan los mismos proveedores que Cursor.

### OpenAI (GPT-4, GPT-4o, etc.)

1. Obtén una API key en [platform.openai.com](https://platform.openai.com/api-keys) (puede ser la misma que configuras en Cursor en *Settings > Models*).
2. Ejecuta con variables de entorno:

```bash
export OPENAI_API_KEY="sk-..."
export LLM_PROVIDER=openai
export OPENAI_MODEL=gpt-4o-mini   # o gpt-4o, gpt-4-turbo, etc.
python3 -m ai_agents.cli "¿Qué prioridades del diagnóstico tocar primero?"
```

### Anthropic (Claude)

1. Obtén una API key en [console.anthropic.com](https://console.anthropic.com/) (puede ser la que usas en Cursor).
2. Ejecuta:

```bash
export ANTHROPIC_API_KEY="sk-ant-..."
export LLM_PROVIDER=anthropic
export ANTHROPIC_MODEL=claude-sonnet-4-20250514   # o claude-3-5-sonnet, etc.
python3 -m ai_agents.cli "¿Qué prioridades del diagnóstico tocar primero?"
```

### Ollama (local, por defecto)

Si no defines `LLM_PROVIDER` o pones `ollama`, se usa Ollama en tu máquina:

```bash
export OLLAMA_MODEL=gemma3:4b   # o el modelo que tengas
python3 -m ai_agents.cli "Tu pregunta"
```

## Variables de entorno resumidas

| Variable | Uso | Ejemplo |
|----------|-----|--------|
| `LLM_PROVIDER` | `ollama` \| `openai` \| `anthropic` | `openai` |
| `OPENAI_API_KEY` | Requerido si provider=openai | `sk-...` |
| `ANTHROPIC_API_KEY` | Requerido si provider=anthropic | `sk-ant-...` |
| `OPENAI_MODEL` | Modelo OpenAI | `gpt-4o-mini` |
| `ANTHROPIC_MODEL` | Modelo Anthropic | `claude-sonnet-4-20250514` |
| `OLLAMA_URL` | Solo ollama | `http://localhost:11434/api/generate` |
| `OLLAMA_MODEL` | Solo ollama | `gemma3:4b` |
| `LLM_TEMPERATURE` | Opcional | `0.2` |
| `LLM_TIMEOUT` | Segundos de espera a la respuesta del LLM (por defecto 300; si Ollama tarda más, súbelo) | `300` |

**Nota:** No pongas API keys en el código; usa variables de entorno o un gestor de secretos.
