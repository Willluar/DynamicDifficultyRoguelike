# Dynamic Difficulty Roguelike

Turn-based grid roguelike in Unity with an adaptive difficulty system that retunes enemy stats and per-element resistances between runs based on logged player performance. Final-year project for BSc (Hons) Games Technology at UWE Bristol (2026).



## Tech

- C# / .NET
- Unity (LTS)
- Turn-based combat on a grid
- A* pathfinding with line-of-sight checks
- Custom telemetry and persistence layer

## What I built

- **DDA manager** — per-element resistance cap, total pool cap, strength multiplier that grows linearly with each analysed run up to +75%, enemy health and damage scaling tied to average stages cleared across the last five runs.
- **Persistence layer** — `RunDataLogger` writes per-run telemetry to disk, replayed on next launch to drive difficulty decisions.
- **Spell system** — element-typed abilities with cooldowns and area-of-effect targeting.
- **A\* pathfinder and line-of-sight** — used by enemy AI for movement and target acquisition.
- **Simulation harness** — `SimulatedPlayerController` runs batched headless games for tuning, so I can iterate on the difficulty curve without playing dozens of full runs by hand. Toggle DDA on/off to compare cleanly.

## Why it's interesting

The DDA system reads its own output. Every run feeds the next, and the harness lets me sanity-check whether the difficulty curve is fair without grinding manual playthroughs. Same patterns as production software: structured data, persistence, state across interacting components, automated test runs.

## Running it

Clone, open in Unity 2022.3+ LTS, open `Scenes/Main`, press Play.

## Contact

[willluar.github.io](https://willluar.github.io) · williamcj@hotmail.co.uk
