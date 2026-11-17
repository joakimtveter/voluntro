# Outbox delivery: per-message failure state, one API instance, no error text stored

This records how the outbox and inbox of ADR-0005 deliver. A module publishes through `IOutbox.Add<TEvent>(event)`, which `ModuleDbContext` implements and the module's narrow context interface inherits. The row goes into the module's own `outbox` table and commits with the command's save. The publisher's `OutboxProcessor<TContext>` calls every consumer in process: for each consumer it opens a new DI scope (running as System), deserializes the payload into the consumer's event type, and runs **Inbox → Transaction → handler** in the *consumer's* context. The inbox decorator skips a message already recorded under `(message_id, consumer)` in the consumer's `inbox`, and records it in the same save as the handler's changes. Attempts and the processed and failed states are kept **once per message**, on the outbox row. The processor assumes **one API instance per database** and claims nothing. The row stores **no error text**: a failure is logged, not persisted.

We chose this over a true inbox per module (the publisher only fans messages out into each consumer's `inbox`, and each module runs its own inbox processor with per-consumer attempts) because an Installation is self-hosted, low-volume, and most events have few consumers. A true inbox doubles the processors and failure states, and its fan-out writes into other modules' schemas. One instance is what self-hosting runs (ADR-0002), and the inbox already keeps consumers safe if that assumption is broken. Exception text can contain personal data, which must not end up in a table nobody cleans up by hand.

## Considered options

- **A true inbox per module with per-consumer state**: a consumer that keeps failing would affect only itself. Can be added later without changing Contracts.
- **Claiming rows with leases (`FOR UPDATE SKIP LOCKED` and a `locked_until`)**: needed only when two instances poll the same database.
- **A `last_error` column**: convenient for an admin view, but a place for personal data to leak to.

## Consequences

- If consumer B succeeds and consumer C keeps failing, the message is redelivered (B skips it through its inbox) and finally the whole message is marked failed, although B handled it. The logs say which consumer failed.
- A failed `Result` (including `common.concurrency_conflict`) and an exception both count as a failed attempt. Failures aren't sorted into transient and permanent. A cancellation caused by the host shutting down doesn't count; the message is retried after the next start.
- Retries back off as `min(2^attempts s, 5 min)`. After `MaxAttempts` (10 by default) the row is marked failed: about 15 minutes. Each failed attempt is a Warning in the log (message ID, type, consumer, attempt, exception) and giving up is an Error.
- Delivery order is not guaranteed: rows are taken oldest first, but a retried message falls behind newer ones.
- The consumer's name in the inbox is the handler's full type name. Renaming a handler while one of its messages is being retried delivers that message once more, which consumers tolerate (ADR-0005).
- A second API instance on the same database must first bring in leases.
- Processed and failed outbox rows, and inbox rows, are deleted after 7 days. A message can't be redelivered once it is processed or failed, so inbox rows need to outlive it only by its retries, which take minutes.
