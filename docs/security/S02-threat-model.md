# S02 threat-model delta

Create and publish deny access unless the server recognises the non-production pilot admin credential. The operator identifier supplied at publication is compared with the persisted draft and approval is obtained through the configured `IOperatorApproval` contract. Lifecycle, verification, audit, and publication fields cannot be mass assigned. Only the fixed published projection is returned.

Approved test operator identifiers are configured server-side; live publication remains blocked. Cookie authentication is not used, so `TR-SEC-005` CSRF protection is not applicable to this pilot credential. Optimistic concurrency and a unique audit constraint prevent stale or duplicate publication facts. Bounded content is escaped by React. Public output is rate-limited, cacheable for 60 seconds, and contains no actor, tenant key, audit, or draft data. The pilot header credential must be replaced by the approved production identity provider before live publication.
