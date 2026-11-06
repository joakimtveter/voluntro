# Identity's emails are sent via integration events

Invitations, email confirmation, password resets and 2FA-reset notices are published by Identity as integration events, each carrying the one-time link token. The Communication module consumes them, renders a localized template and sends the email. We chose this over a synchronous "send email" contract call so that creating the user and queuing the email happen in **one transaction**: you can never have a user without their invite email, or an email for a user that was rolled back.

## Consequences

- One-time tokens briefly sit in the outbox. They are short-lived (reset: 1 hour, invite/confirmation: 7 days), and processed outbox rows are deleted after ~7 days.
- Communication's sending core (`IEmailSender`, SMTP via MailKit, Fluid templates) is part of the core platform, not a later module.
