# NoorPath Icon System

## Decision

NoorPath uses one internal typed SVG registry: `NoorPathIcon`.

The repository did not contain an application icon dependency when VS-18 began. The internal registry avoids an unnecessary dependency and lockfile change while enforcing one 24-pixel grid, one stroke treatment and one accessible API. Consumers reference semantic names rather than importing arbitrary SVGs.

## Rules

- SVG only; no emoji icons.
- Default grid: 24 × 24.
- Default stroke: 1.75, rounded line caps and joins.
- Decorative icons are hidden from assistive technology.
- Meaningful standalone icons require a title or an explicit accessible label on the control.
- Icon-only controls must still have a visible tooltip where appropriate and an accessible name.
- Operators and content editors cannot select or upload replacement icons for standard concepts.
- New icons require design-system review and must match the grid, stroke and optical weight.

## Fixed package mapping

| Customer concept | Icon name |
| --- | --- |
| Return Flights | `plane` |
| Umrah Visa Included | `passport` |
| Makkah Hotel / Madinah Hotel | `hotel` |
| Airport or Intercity Transport | `bus` |
| Daily Meals Included | `meal` |
| Group Leader and Support | `support` |
| Travel Kit / Umrah Kit | `bag` |
| Journey Payment Schedule | `payment` |
| Cancellation Policy / Protection | `shield` |
| Verified Operator | `verified` |

## Fixed status mapping

| Status meaning | Icon name |
| --- | --- |
| Completed / approved | `check` |
| Action required / pending warning | `warning` |
| Error / rejected | `error` |
| Information / neutral | `info` |

## Support mapping

| Action | Icon name |
| --- | --- |
| WhatsApp Support | `whatsapp` |
| Request a Callback | `phone` |
| General assisted support | `support` |

## Occupancy

Double, triple and quad sharing use the governed `OccupancyAvatarGroup` component. The repeated avatar treatment is presentation-only and exposes a single readable description such as “3 pilgrims sharing one room.”
