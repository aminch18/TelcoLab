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
- [x] **v1** — full lifecycle state machine (active → porting → rejected/cancelled), a simulated
  clearing-house third party (async webhook), request/webhook correlation, reminder-based
  retry & timeout, webhook authentication, and unit tests
- [x] **v2** — porting results delivered over Orleans Streams, with a second (audit) consumer
  subscribing to the same stream to demonstrate fan-out
- [ ] **v3** — multi-silo clustering with a real clustering provider
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

## Articles

The design decisions behind this repo are written up as a series:

0. One workflow, two architectures: actors vs. queues (introduction) —
   [English](docs/articles/00-porting-two-architectures.en.md) ·
   [Español](docs/articles/00-porting-two-architectures.es.md)
1. Building the number-porting workflow with Orleans grains —
   [English](docs/articles/01-porting-with-orleans.en.md) ·
   [Español](docs/articles/01-porting-with-orleans.es.md)
2. From a direct call to Orleans Streams (fan-out) —
   [English](docs/articles/02-porting-with-streams.en.md) ·
   [Español](docs/articles/02-porting-with-streams.es.md)

Each article also has a rendered PDF beside it in [docs/articles/](docs/articles).

## License

MIT — see [LICENSE](LICENSE).
