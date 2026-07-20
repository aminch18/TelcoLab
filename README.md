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
- [x] **v3** — durable state, reminders and clustering on PostgreSQL (ADO.NET providers), with a
  multi-silo cluster verified across two nodes
- [ ] **v4** — Observability (Orleans Dashboard + OpenTelemetry)
- [ ] **v5** — Docs pass, linked from the accompanying article series

## Running

### Full stack in containers (recommended)

Postgres + the simulated third party + a two-silo cluster, deployable to any container
environment. The Orleans schema is created automatically from `db/`:

```bash
docker compose up --build
```

Drive it against either silo — a subscription is the same actor whichever one you call:

```bash
curl -XPOST localhost:5100/subscriptions/+34600000011/activate
curl -XPOST localhost:5100/subscriptions/+34600000011/port \
     -H 'content-type: application/json' -d '{"donorOperator":"ACME"}'
curl localhost:5101/subscriptions/+34600000011          # same grain, other silo
```

Each silo exposes `/health` and `/health/ready` for orchestrator probes.

### Single silo, no infrastructure

In-memory storage (lost on restart) — used by the unit tests and the quick local demo, so no
Docker is needed:

```bash
dotnet run --project src/TelcoLab.Api
```

Without a `ConnectionStrings:Postgres` value the silo falls back to in-memory clustering and
storage; set it (and `Orleans:AdvertisedIP`) to run durable and clustered.

## Project layout

The domain is organised by aggregate; the API host is organised by feature (vertical slices),
with endpoints built on [MinApiLib](https://github.com/fernandoescolar/MinApiLib) — one record
per endpoint, auto-discovered.

- `src/TelcoLab.Domain` — the domain, by aggregate: `Subscriptions/`, `Auditing/` (grains,
  state, contracts)
- `src/TelcoLab.Api` — the silo host, by feature: `Features/{ActivateSubscription, RequestPorting,
  GetSubscription, GetAudit, ClearingWebhook}/` + `Infrastructure/`
- `src/TelcoLab.ClearingHouse` (+ `.Contracts`) — the simulated third party
- `db/` — Orleans PostgreSQL setup scripts, run automatically by Postgres on first start
- `tests/TelcoLab.Tests` — grain state-machine and stream tests

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
3. From one silo to a real cluster on Postgres —
   [English](docs/articles/03-clustering-and-storage.en.md) ·
   [Español](docs/articles/03-clustering-and-storage.es.md)
4. When *not* to reach for Orleans (the honest scorecard) —
   [English](docs/articles/04-when-not-to-use-orleans.en.md) ·
   [Español](docs/articles/04-when-not-to-use-orleans.es.md)

Each article also has a rendered PDF beside it in [docs/articles/](docs/articles).

## License

MIT — see [LICENSE](LICENSE).
