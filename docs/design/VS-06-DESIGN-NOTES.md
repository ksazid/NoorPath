# VS-06 Package Details — Design Notes

Status: Implementation notes; rendered Product Owner acceptance remains required.

## Authority

1. `design-references/noorpath-package-reference.png`
2. approved Landing reference for shared customer chrome
3. `design-system/MASTER.md`
4. existing merged Package implementation
5. UI UX Pro Max for states/accessibility/responsive behavior
6. Impeccable for bounded hierarchy/spacing/content hardening
7. Emil design-engineering principles for purposeful feedback only
8. Ponytail full mode for minimum-change implementation

No skill is permitted to redesign NoorPath or replace the approved Package visual language.

## VS-06 visual decision

VS-06 is a **truth migration**, not a redesign.

The existing Package composition remains recognizable:

- customer header;
- dual Haramain gallery;
- operator and stay summary column;
- right-side commercial summary card;
- three-column journey/content/status area on desktop;
- gold/green/ivory/black visual language;
- fixed lower action summary;
- existing responsive collapse behavior.

The content inside those regions changes only where the old page used static preview or unsupported claims.

## Truth replacements

| Existing placeholder/static treatment | VS-06 treatment | Reason |
| --- | --- | --- |
| Static preview package lookup | Public departure-detail API by `departureId` | Customer detail must match the discovered published departure |
| IATA / ISO / years-in-business badges | Origin, journey duration, departure date | No approved public operator-credential contract currently supports the old claims |
| Fixed `Quad Sharing` accommodation copy | Published stay classification + confirmation state | Occupancy cannot be inferred from package content |
| Fabricated seven-step itinerary | Published dates, route summary and operator-entered travel details | Catalogue does not currently own day-by-day itinerary facts |
| Fixed inclusion/travel-kit/Umrah-kit lists | Complete ordered published inclusions and exclusions | Customer content must come from Catalogue truth |
| Placeholder payment summary | Published occupancy prices + current availability | VS-06 exposes price/availability, not a quote/payment schedule |
| `On request` availability | Current per-occupancy Inventory state | Availability is operational truth |
| Static cancellation/refund values | Explicit pre-commitment disclosure boundary | Cancellation/payment policies are not yet frozen for this slice |

## Customer states

### Loading

Preserve a substantial detail-page footprint so the customer does not see the page collapse while authoritative data loads. Announce loading through `aria-busy`/live semantics.

### Published / saleable

Show:

- operator identity;
- package name and summary;
- origin and published dates;
- both holy-city stays independently;
- confirmation state for each stay and travel;
- full inclusions/exclusions;
- every published occupancy price;
- current per-occupancy availability;
- human-support path.

No artificial urgency or `limited` label is introduced.

### Unavailable / not found

Unknown, unpublished, operator-ineligible and no-longer-saleable resources share one calm public state. The UI does not explain internal lifecycle or operator state and provides a path back to currently published packages.

### Error / offline-equivalent

Network/server failure is distinct from business unavailability. Keep a retry action and show a safe correlation reference when the API supplies one.

## Responsive intent

At 390 px and 360 px:

- retain the established detail header and gallery;
- operator, package, stay confirmation, price and availability remain visible;
- occupancy rows stack only as needed and never require horizontal scrolling;
- fixed bottom summary remains reachable and exposes the published starting price and availability count;
- important interactive targets remain at least 44 CSS px;
- 200% text sizing must reflow without clipping.

## Accessibility

- semantic main/section/nav structure remains;
- visible focus comes from the existing global customer baseline;
- pending/confirmed and available/unavailable states are expressed in text, not colour alone;
- loading/error/not-found states have appropriate live/status semantics;
- images retain meaningful descriptive alt text;
- reduced-motion behavior stays restrained; no new decorative motion is introduced.

## Motion decision

No new entrance animation, carousel behavior or decorative transition is required. Existing press feedback is retained. State transitions caused by data loading are immediate because clarity and perceived responsiveness outrank decoration for this high-information surface.

## Still requiring rendered review

Before VS-06 merges, capture and review:

- desktop populated Package page against approved Package reference;
- 390 px populated page;
- 360 px populated page;
- pending-fact rendering;
- unavailable/not-found state;
- error/retry state;
- keyboard focus path;
- 200% text reflow;
- reduced-motion behavior.

A passing source/CI check is not Product Owner visual acceptance.
