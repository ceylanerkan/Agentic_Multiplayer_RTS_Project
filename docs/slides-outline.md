# Slides outline (about 12 slides, 10 minutes)

1. **Title**: project name, your name, course, "one-person team (instructor approved)".
2. **Idea**: two-player RTS + an AI commander that advises, never acts alone. One screenshot of the game with the AI panel.
3. **Game design**: map with 10 zones, 3 resources, 8 units with counter loops (small diagram), win condition; one in-game
   screenshot with the blue/red team discs and labels (placeholder graphics, readable by design).
4. **Architecture**: the Mermaid diagram from `docs/design-brief.md` (Unity <-> gateway <-> agent <-> MCP server <-> SQLite/RAG).
5. **Multiplayer**: Fusion Host Mode; what is replicated; `CommandRelay` RPC path; why Host Mode.
6. **MCP tools**: the table of 7 read-only tools; "no tool takes a player id".
7. **Agent loop and structured plan**: steps, tool log excerpt, `RTSActionPlan` fields.
8. **RAG**: corpus (33 chunks), BM25 (+ optional embeddings), source ids shown as evidence.
9. **Human control**: Approve / Reject / Ask why, `GameActionValidator` re-check, decision log.
10. **Security**: fog of war before the network, schema rejects unknown fields, identity from the session, injection test; the
    client-side fog limitation.
11. **Evaluation**: metrics table (mock vs live), one failure case.
12. **Lessons and future work**: engine-free core, single source of numbers, "a compile check proves types, not lifecycle" (the
    unregistered spawner); server-side fog, plan chaining, unit separation.
(Backup slide: demo scenario checklist 1-9.)
