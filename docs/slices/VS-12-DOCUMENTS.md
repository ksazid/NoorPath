# VS-12 — Documents

## Status

**Definition of Ready approved for implementation on 2 August 2026.**

The Product Owner approved the pilot policy below. Runtime implementation may proceed, but production document storage remains disabled until legal/privacy confirms the retention schedule and security validates the deployed S3 and ClamAV controls.

## Slice and requirements

- Slice: **VS-12 Documents**.
- Requirements: **INV-DOC-001** through **INV-DOC-006**, **POL-DOC-001**, **POL-DOC-002** and the Document & Passport Security baseline.
- Depends on: **VS-01 Operator Access** and **VS-11 My Journey**.

## Outcome

Required traveller documents can be uploaded securely, reviewed by an authorized operator, corrected when needed and reflected truthfully in per-traveller readiness.

## Actors and authorization boundary

- An authenticated customer may manage submissions only for an account-owned traveller attached to an eligible booking.
- An operator user may access or review a submission only with an active membership, explicit document-review permission and the booking's operator scope.
- Resource authorization is enforced server-side on every metadata, upload, access and review operation. Knowledge of an identifier or object key grants no authority.

## Domain ownership

- **Documents** owns versioned requirements, submission metadata, validation state, review state and append-oriented review history.
- **Traveller** supplies the existing traveller identity; Documents does not create another traveller record.
- **Booking** supplies the confirmed booking and operator relationship used for authorization.
- A private object store holds encrypted content. The application exposes only short-lived, purpose-bound authorized access after safety checks.

## Required workflow

1. My Journey shows requirements separately for each traveller.
2. The customer selects one requirement and submits an allowed file through the approved private-upload mechanism.
3. The submission remains quarantined while declared type, size, content signature and malware status are validated.
4. Only a safe submission can become available to an authorized reviewer.
5. The reviewer approves, requests a reasoned correction or rejects the submission.
6. A customer resubmission creates a new submission while preserving the previous submission and review chain.
7. Booking-level document readiness is derived only when every applicable traveller requirement is approved.

## States

Each traveller requirement distinguishes:

- not submitted;
- uploading;
- validating/quarantined;
- under review;
- approved;
- correction required;
- rejected/invalid; and
- superseded by resubmission.

The customer and operator experiences also cover loading, empty, offline, retryable error, permission denied, stale/concurrent review and safe unavailable states. Labels and explanations do not rely on colour alone.

## Privacy, audit and telemetry

- Document content, extracted identity data, object keys and signed URLs are Highly Sensitive.
- Objects are private, encrypted in transit and at rest, non-enumerable and never exposed through permanent URLs.
- Authorized document access and every review transition are audited with actor, purpose/reason and timestamp.
- Ordinary logs, analytics, integration events and customer-safe projections contain no content, signed URL, object key or unnecessary personal data.
- Unsafe or indeterminate files remain quarantined and inaccessible to reviewers.
- Production documents are never copied to lower environments.

## Approved pilot policy

1. **Required documents and versioning** — policy version `v1` requires a passport bio page and passport photo for each traveller. The applicable policy version and requirements are snapshotted when the booking becomes eligible; later policy versions affect only new snapshots and never silently rewrite existing bookings.
2. **Retention and deletion** — encrypted document objects are deleted 30 days after journey return or booking cancellation, whichever applies. Non-sensitive audit metadata is retained for one year. A recorded legal/operational hold pauses deletion, and every deletion or hold transition is audited. Production enablement requires legal/privacy confirmation of this schedule.
3. **Private storage and access** — use a private encrypted AWS S3 bucket with blocked public access and opaque non-enumerable object names. Customers upload directly through a purpose-bound presigned PUT URL and authorized users view through a purpose-bound presigned GET URL; both expire after five minutes. The API stores no permanent public URL and authorizes every request before signing.
4. **Malware and file constraints** — accept PDF, JPEG and PNG files up to 10 MB. Verify declared type, signature and size, keep every upload quarantined, and scan with ClamAV. Scanner timeout, error or indeterminate results fail closed: the object remains quarantined and inaccessible to reviewers until a successful rescan or audited deletion.

These decisions are frozen for VS-12 implementation. Provider configuration, threat-model evidence and legal/privacy confirmation remain production-enablement gates rather than reasons to invent different runtime behaviour.

## Acceptance criteria

The acceptance contract is recorded in `delivery/slices/VS-12.json`. Every criterion remains open until the blockers above are resolved and the end-to-end implementation is certified on the exact final SHA.

## Explicit exclusions

- Visa lifecycle and official visa decisions.
- OCR, automated extraction, biometric matching or automated approval.
- Permanent public URLs, direct storage browsing or lower-environment production data.
- A generic case-management system or unrestricted operator file browser.

## Merge rule

This specification may merge to record the approved implementation contract. **No VS-12 runtime implementation may merge** until every checklist gate passes on the exact final SHA, rendered evidence is reviewed and Product Owner acceptance is recorded. Production document storage must remain disabled until the production-enablement gates above are evidenced.
