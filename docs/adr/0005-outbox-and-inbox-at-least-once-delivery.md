# Modules integrate through an outbox/inbox with at-least-once delivery

Modules never share tables. When one module must react to another, the publisher writes an integration event to its **own** `outbox` table in the same transaction as the state change. A per-module background processor delivers it in process, and each consumer records `(message_id, consumer)` in its `inbox` to stay idempotent. Synchronous reads go through small query interfaces in Contracts. This was chosen over shared read access to tables (coupling at the database level) and over synchronous calls only (lost work when one side fails). It also maps directly onto a message broker if modules become services.

## Consequences

- Delivery is at least once, so every consumer must be idempotent.
- Event names are stable and versioned (`identity.user_registered.v1`). Changing a payload means a new version.
- Processing starts with polling only; Postgres `LISTEN/NOTIFY` can be added if the latency matters.
- Processed outbox rows are deleted after ~7 days, because some payloads carry one-time tokens (see ADR-0009).
