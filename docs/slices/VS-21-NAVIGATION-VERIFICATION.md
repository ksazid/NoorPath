# VS-21 Navigation Verification

| Source | Destination | Expected | Current evidence |
|---|---|---|---|
| `/account` unauthenticated | `/auth/login` | Begin Auth0 transaction and return to `/account` | Pending repaired preview |
| Auth0 callback | `/account` | Persist session and render My NoorPath | Pending repaired preview |
| `/journeys` | owned journey | Render `DEMO-LKO-001` only | Pending identity session |
| owned journey | documents, visa, payment, cancellation | Render customer-safe state | Pending identity session |
| `/operator` | operator queues | Allow approved member with explicit permissions | Pending identity session |
| `/admin` | administrator shell | Allow configured platform administrator | Pending identity session |
| `/platform/publications` | publication detail | Allow configured publication approver | Pending identity session |
| foreign booking deep link | safe not found | Do not disclose foreign account data | Pending identity session |
| logout | sign-in boundary | Remove session and deny protected routes | Pending repaired preview |

No route is considered passed until executed against the exact deployed VS-21 head with the retained Auth0 test identity.
