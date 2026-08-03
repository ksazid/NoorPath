# NoorPath Cross-Platform Design Rule

## Governing rule

No new NoorPath web or mobile screen may introduce a different visual language unless the shared design system itself has been deliberately updated and approved by the Product Owner.

## Shared identity

Web and mobile must use the same approved NoorPath brand language, including:

- logo and brand assets
- colour and semantic tokens
- typography principles
- spacing and sizing scale
- border radius and elevation language
- iconography and imagery
- tone of voice and customer terminology
- status, feedback, validation, loading, empty, error, offline, and success patterns

The approved Landing and Package references, `design-system/MASTER.md`, approved Figma artifacts, and shared design tokens remain the visual source of truth.

## Platform adaptation

Consistency does not require pixel-for-pixel duplication. Each platform must use its appropriate interaction conventions while preserving NoorPath's identity and journey model.

Examples:

- Web may use headers, side navigation, hover, responsive grids, and keyboard-first interactions.
- Mobile may use bottom tabs, native gestures, safe areas, platform pickers, secure device capabilities, and touch-sized controls.
- Layouts may be reflowed, simplified, or sequenced differently where device constraints require it.

Platform adaptation must not change product meaning, pricing presentation, workflow state, trust language, or the relationship between primary and secondary actions.

## Shared implementation assets

Where technically appropriate, web and mobile should consume the same source definitions for:

- design tokens
- semantic colours
- typography roles
- icons and approved assets
- API contracts and state labels
- validation messages and customer-facing terminology

Web and mobile UI components should not be shared mechanically when native platform behavior would be compromised.

## Review requirement

Every new or materially changed web or mobile screen must be reviewed against:

1. the approved NoorPath visual references;
2. the shared design-system rules;
3. the equivalent journey on the other platform, when one exists;
4. accessibility and platform-native usability requirements; and
5. Product Owner approval for any material visual-language change.

A screen is not complete merely because it passes source tests; visual comparison and platform-appropriate interaction review are required.
