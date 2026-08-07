# VS-23 — Operator Draft Package Builder

## Goal

Give approved operator staff a fast, guided package-authoring flow that reuses the existing catalogue, departure, commercial, payment-plan and publication-review architecture.

## UX contract

- The staff shell, navigation hierarchy, typography, spacing and NoorPath visual language remain unchanged.
- `/operator/packages` is the package-management starting point.
- `/operator/packages/new` is the canonical package-draft entry point.
- The flow supports blank creation and cloning an operator-owned package.
- Draft progress is visible, saveable and private at every stage.
- Preview must render as a natural extension of the approved customer package-details page.

## Standard package facts

Platform-owned terminology must be used consistently:

- Return flights
- Makkah stay
- Madinah stay
- Visa included
- Intercity travel — bus or train
- Local transfers
- Breakfast, lunch and dinner
- Ziyarah
- Umrah guidance
- Travel kit
- Umrah kit

Selecting an inclusion removes the same item from exclusions. Operators may add factual custom items, but may not rename standard platform terminology.

## Draft flow

1. Start blank or clone.
2. Set origin and travel dates.
3. Calculate days and nights and suggest the heading.
4. Confirm Makkah and Madinah stays.
5. Select standard inclusions and intercity travel mode.
6. Configure price, capacity and booking amount.
7. Choose instalments or one final balance after booking.
8. Review suggested editable milestones.
9. Preview the customer-facing package.
10. Submit now or schedule submission for platform review.

## Publication rule

The operator sees the minimum approval window before submission. Operator submission never publishes directly; platform approval remains mandatory.

## Security and tenancy

- All reads and writes remain operator-scoped.
- Clone source must belong to the authenticated operator.
- Drafts are private until platform approval completes.
- Existing optimistic concurrency and audit behavior remain in force.
