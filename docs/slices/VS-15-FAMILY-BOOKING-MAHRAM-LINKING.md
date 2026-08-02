# VS-15 — Family Booking & Mahram Linking

## Outcome

An authenticated NoorPath customer can maintain an account-owned family travel party, explicitly link travellers through Mahram relationships where required, validate the party against a versioned policy, and carry an immutable validated snapshot into quote, booking and My Journey flows.

## Product boundary

NoorPath records customer-declared relationships and evaluates them against an approved configurable product policy. It does not issue a religious or legal ruling and must display a clear customer-facing disclaimer wherever eligibility is shown.

## Domain ownership

- **Travellers** owns traveller identity, profile facts, account ownership and archive state.
- **Family Booking** owns party membership, relationship declarations, validation results and policy-version references.
- **Pricing** consumes traveller composition and occupancy facts.
- **Booking** copies an immutable party and relationship snapshot at checkout.
- **Documents** and **Visa** consume traveller identifiers and customer-safe relationship projections only.

No module may directly update another module's tables.

## Core model

### FamilyParty

- `Id`
- `AccountId`
- `Name`
- `Status`: `Draft`, `Validated`, `Archived`
- `PolicyVersion`
- optimistic concurrency `Version`
- created/updated timestamps

### FamilyPartyMember

- `PartyId`
- `TravellerId`
- active/removed state
- created/removed timestamps

A traveller may appear only once in an active party membership.

### MahramRelationship

- `Id`
- `PartyId`
- `FromTravellerId`
- `ToTravellerId`
- `RelationshipType`
- customer declaration timestamp
- active/revoked state
- optimistic concurrency `Version`

Self-links, duplicate active links, cross-account links, archived travellers and travellers outside the party are invalid.

### PartyValidation

- party identifier and version
- policy version
- outcome: `Valid`, `ActionRequired`, `Invalid`
- issue codes and customer-safe guidance
- evaluated timestamp

Validation is invalidated whenever membership or a relationship changes.

## Approved relationship types

The runtime must use a configurable allow-list rather than hard-coded UI assumptions. The initial product vocabulary is:

- spouse
- father
- son
- brother
- grandfather
- grandson
- paternal uncle
- maternal uncle
- nephew
- father-in-law
- son-in-law
- stepfather
- stepson
- foster relationship where permitted by the configured policy

The policy engine determines which direction and traveller facts are relevant. UI labels must remain understandable and must not expose internal enum names.

## Required flows

### Manage travellers

Customers can view, add, edit and archive travellers owned by their account. Existing VS-07 traveller rules remain authoritative.

### Build a family party

Customers create a named party, add existing account-owned travellers and remove members while the party is not attached to a confirmed booking.

### Link a Mahram

Customers select two party members, choose an allowed relationship type, confirm the declaration and save it. The API validates ownership, party membership, active traveller state, uniqueness and concurrency.

### Validate party

The customer can request validation. The service evaluates the current party against the configured policy version and returns stable issue codes with customer-safe corrective guidance. A successful result records the policy version and exact party version.

### Quote and booking integration

Quote and booking commands accept a validated party identifier. The server rechecks account ownership, current party version and policy validity. Booking stores an immutable traveller and relationship snapshot plus policy version. Later party edits do not mutate the booking snapshot.

### My Journey

Confirmed journeys show booked travellers and a customer-safe Mahram summary derived from the immutable booking snapshot. Sensitive relationship notes or document evidence are not shown.

## API contract

Minimum account-scoped endpoints:

- `GET /api/v1/family-parties`
- `POST /api/v1/family-parties`
- `GET /api/v1/family-parties/{partyId}`
- `PATCH /api/v1/family-parties/{partyId}`
- `POST /api/v1/family-parties/{partyId}/members`
- `DELETE /api/v1/family-parties/{partyId}/members/{travellerId}`
- `POST /api/v1/family-parties/{partyId}/mahram-links`
- `DELETE /api/v1/family-parties/{partyId}/mahram-links/{linkId}`
- `POST /api/v1/family-parties/{partyId}/validate`

Foreign-account resources return safe not-found responses. Mutations require the expected optimistic-concurrency version.

## Security and privacy

- Account ownership is enforced server-side for every read and mutation.
- Logs and telemetry use opaque identifiers and never record names, dates of birth, passport details or relationship notes.
- Relationship declarations are private customer data and are not exposed to unrelated operators.
- Audit records capture actor, action, target, reason where applicable, outcome, correlation identifier and timestamp.
- The UI must include a clear disclaimer that final travel, visa and religious requirements may require independent verification.

## Accessibility and experience

The customer flow must extend the established NoorPath landing/package visual language and shared footer. It must include complete loading, empty, validation-required, stale, not-found and recoverable-error states and pass keyboard, focus, 200% text, mobile reflow, reduced-motion and serious/critical axe checks.

## Explicit exclusions

- Automated religious or legal ruling.
- Kinship-document verification or OCR.
- Cross-account traveller sharing or invitations.
- Operator override of Mahram policy.
- Room allocation, seat assignment or new pricing rules.
- Mutating an immutable booked family snapshot.

## Merge rule

Runtime implementation remains Draft until exact-head CI, migration, architecture, concurrency, rendered accessibility, privacy/security and Product Owner gates pass on the unchanged certified SHA.
