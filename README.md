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
- [x] M1 design brief, knowledge base, schemas: see [docs/design-brief.md](docs/design-brief.md)
- [ ] M2 networked core RTS
- [ ] M3 combat, tech, fog of war, win condition
- [ ] M4 MCP server, agent, RAG
- [ ] M5 Unity AI integration, approval, security
- [ ] M6 evaluation, report

## Setup
See [docs/setup.md](docs/setup.md). Secrets (Gemini API key, Photon App ID) are never committed; copy
`.env.example` to `.env`.
