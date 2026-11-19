# How to use these prompts

1. Put this kit in the Unity project root so `README.md`, `CLAUDE.md`, `docs/`, `prompts/` and `reference/` sit beside `Assets/`.
2. Open a terminal in that root and run `claude`.
3. Paste `00_kickoff.md`. Answer its questions. Say "go M1".
4. For each milestone: paste the prompt file, wait for the report (test table, screenshots), review it, then say "go" for the next one. If a milestone fails its tests, say "fix the failures and re-run" before moving on.
5. Between milestones run `/clear` if the conversation gets long; the README and docs reload automatically through `CLAUDE.md`.
6. Run `11_FINAL_ACCEPTANCE.md` after M10, and any time you want a health check.
7. Ask for the report format every time: what was built, test table, what could not be verified, next step.
