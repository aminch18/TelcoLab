# TelcoLab

A reference implementation exploring how a telco-style subscription lifecycle (activation, number porting, rejection, cancellation) maps onto [Microsoft Orleans](https://github.com/dotnet/orleans)' virtual actor model, instead of a traditional queue-driven, bounded-context service architecture.

This is a learning/demo project — the domain is intentionally generic, not tied to any real operator.

## Why Orleans for this domain

A subscription's lifecycle is naturally an actor: it has identity, owns its own state, and reacts to events over time (porting requested, porting rejected, porting cancelled). The architectural bet this project explores:

| Traditional pattern | Orleans equivalent |
| --- | --- |
| Entity + row in a database, read/written by services | Grain — identity and state are the same thing |
| Message queue + worker that polls/consumes | Orleans Streams — the grain subscribes and reacts |
| Event handler methods external to the entity | State machine transitions live inside the grain |
| Scheduled retry/timeout jobs | Grain Reminders / Timers |
| Bounded contexts as separate services | Grain interfaces grouped by domain, same or separate silos |
| Repository pattern over SQL/EF | `IPersistentState<T>` with a pluggable storage provider |

## Status

- [x] **v0** — minimal silo + `SubscriptionGrain` with persistent state
- [ ] **v1** — full subscription lifecycle state machine (active → porting → rejected/cancelled) + tests
- [ ] **v2** — porting events modeled as Orleans Streams
- [ ] **v3** — Reminders for retries/timeouts
- [ ] **v4** — Observability (Orleans Dashboard + OpenTelemetry)
- [ ] **v5** — Docs pass, linked from the accompanying article series

## Running locally

```bash
dotnet run --project src/TelcoLab.Silo
```

Starts a single-node localhost silo with in-memory grain storage and activates a demo `SubscriptionGrain` on startup.

## Project layout

- `src/TelcoLab.Abstractions` — grain interfaces and state contracts
- `src/TelcoLab.Grains` — grain implementations
- `src/TelcoLab.Silo` — silo host

## License

MIT — see [LICENSE](LICENSE).
