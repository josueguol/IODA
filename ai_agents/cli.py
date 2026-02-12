from __future__ import annotations

import argparse
from pathlib import Path

from ai_agents.config import default_config
from ai_agents.orchestrator import Orchestrator


def main() -> None:
    parser = argparse.ArgumentParser(description="Orquestador de agentes (RAG + memoria) para el CMS.")
    parser.add_argument("question", help="Pregunta del usuario sobre el proyecto/CMS.")
    parser.add_argument(
        "--root",
        default=str(Path(__file__).resolve().parents[1]),
        help="Ruta raíz del workspace (por defecto: padre del paquete).",
    )
    args = parser.parse_args()

    cfg = default_config(args.root)
    orch = Orchestrator(cfg)
    out = orch.answer(args.question)

    print(out.answer)
    print("\n---")
    print(f"Agentes: {', '.join(out.selected_agents)}")
    if out.citations:
        print("Citas (paths):")
        for c in out.citations[:10]:
            print(f"- {c.get('path')}#{c.get('start')}:{c.get('end')}")


if __name__ == "__main__":
    main()

