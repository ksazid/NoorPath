# VS-34 — Platform Administration & Persona Certification

## Outcome

Finish the remaining NoorPath platform-administration requirement without deploying to production or coupling completion to the deferred customer OTP provider or Knowledge Pack.

A configured Platform Administrator can review operator lifecycle state, make governed approval decisions and inspect append-only decision history. Demo/test identities certify Customer, Operator and Platform Administrator authority boundaries through the same deny-by-default access paths used by the application.

## Scope

### Platform Administrator command centre

- Replace the placeholder `/admin` shell with an operational command centre that preserves NoorPath's existing protected-account visual language.
- Show operator lifecycle summary and a pending-first queue.
- Provide explicit allowed transitions rather than arbitrary state editing.
- Require reasons for rejection, suspension and deactivation.
- Surface stale/error/success feedback and operator decision history.
- Keep existing publication review reachable from the platform navigation.

### Operator approval lifecycle

The authoritative lifecycle remains in the Operators module:

`Draft -> PendingApproval -> Approved | Rejected`

`Rejected -> PendingApproval`

`Approved -> Suspended | Deactivated`

`Suspended -> Approved | Deactivated`

`Deactivated` is terminal.

All accepted transitions increment the operator version and append an audit record containing actor account, previous state, target state, reason where supplied, resulting operator version, timestamp and correlation identifier.

Operator application access remains fail-closed: an active member must belong to an `Approved` operator and hold the explicit required permission.

## Persona certification

The existing development/test authentication scheme is used only in Development/Test and accepts `X-NoorPath-Test-Identity`. VS-34 uses that mechanism to certify persona boundaries without representing customer phone OTP as complete.

Expected demo identities:

- `customer-account`: account access allowed; operator and platform administration denied.
- `approved-account`: account and approved operator access allowed; platform administration denied.
- `platform-administrator`: account and platform administration allowed; operator administration denied unless separately provisioned as an operator member.
- A pending demo operator member: operator access denied before approval and allowed after the Platform Administrator transitions its operator to `Approved`.

Existing Customer and Operator rendered journeys remain part of regression certification. VS-34 adds the missing Platform Administrator rendered workflow and cross-persona authorization proof.

## Security and privacy

- Platform operator endpoints require authentication and an exact configured Platform Administrator account ID.
- Unknown or unauthorized identities receive no operator administration data.
- State writes require the expected current version.
- Adverse decisions require a bounded reason of at most 500 characters.
- Operator history is append-only for this workflow.
- No customer OTP credential, phone number, passport data or new speculative personal information is introduced.

## Deferred explicitly

- Customer phone OTP provider selection/configuration and production authentication cutover.
- Knowledge Pack.
- Agent portal.
- External supplier/airline/hotel/insurance/WhatsApp integrations.
- Production deployment.

## Product completion rule

VS-34 is complete when exact-head CI, slice governance, rendered review, navigation reachability, migration/model parity, persona authorization integration tests and existing Customer/Operator regression suites pass. Production remains on its separately approved SHA.
