# CSE-377 Agentic Multiplayer RTS

Age-of-Empires-inspired two-player RTS prototype built with **Unity 6** and **Photon Fusion 2 (Host Mode)**.
It includes a runtime **AI Strategic Commander**: a Gemini-based agent that inspects game state through a
custom **MCP server**, retrieves game knowledge with **RAG**, and returns **schema-validated action plans**.
The player must approve every plan before it runs, and plans go through the same authoritative path as
normal player commands.

> Team note: this project is developed by a single student (team size exception), so the commit history
> has one author.

## Repository layout
```
UnityRTS/          Unity 6 project (Fusion 2)
ai/mcp-server/     Custom RTS MCP server (Python)
ai/gateway/        HTTP gateway + agent runner (Gemini function calling + MCP client)
ai/schemas/        action_plan.schema.json, safe_snapshot.schema.json
ai/rag/            knowledge base, index builder, retriever
evaluation/        prompts, fixtures, eval runner, results
docs/              design brief, architecture, setup, report
```

## Status
Detailed list of the remaining work: [TODO.md](TODO.md).

- [x] M1 design brief, knowledge base, schemas: see [docs/design-brief.md](docs/design-brief.md)
- [x] Unity project setup: Unity 6 (URP), Photon Fusion 2.0.13, ParrelSync, Newtonsoft JSON, AI Navigation,
      Run In Background, Photon App ID git-ignored
- [ ] M2 networked core RTS: Host/Join lobby, selection, move/attack/gather/build/train commands, HUD
- [ ] M3 combat, tech, fog of war, event log, win condition, two-instance test video/log
- [ ] M4 MCP server (7 tools + tests), agent with tool log, RAG index, AI gateway
- [ ] M5 Unity AI integration, approval, validator, security tests
- [ ] M6 evaluation, IEEE report, slides, Windows build, backup demo video

## Setup
See [docs/setup.md](docs/setup.md). Secrets (Gemini API key, Photon App ID) are never committed; copy
`.env.example` to `.env`.
