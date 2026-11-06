# One installation serves one organization

Voluntro is self-hosted, and each installation serves exactly one Organization, so there is **no TenantId** on any table and no tenant filtering. If Voluntro becomes SaaS later, the plan is **database-per-tenant**: each Organization gets its own database, and only connection-string resolution changes. That avoids retrofitting row-level tenancy onto every query. It also gives strong data isolation, which matters for children's data under GDPR.

## Consequences

- Nothing specific to an organization may be hard-coded: name, logo, locale, time zone, enabled modules and providers all live in the database or configuration.
- In-memory caches (module activation, permissions) are per-process and assume one Organization. They will need a tenant key if SaaS happens.
