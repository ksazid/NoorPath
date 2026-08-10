# VS-36 — Navigation Verification

| Journey | Expected result | Evidence |
| --- | --- | --- |
| Home / discovery → `/packages/{departureId}` | Package Details loads with Package reference hierarchy | Pending certification |
| Current Package Details → available same-origin date | Navigates to `/packages/{siblingDepartureId}` | Pending certification |
| Browser Back after sibling date | Returns to previous package and selected date | Pending certification |
| Package Details → Travel date Change | Moves focus/scroll target to Available Travel Dates | Pending certification |
| Package Details → Guests | Expands/collapses guest categories without route change | Pending certification |
| Package Details → Room Sharing | Updates supported adult guest count and authoritative financial preview | Pending certification |
| Package Details → Milestone / Pay Later | Updates presentation only; route remains Package Details | Pending certification |
| Package Details → Book now | Opens existing planner with `occupancy` and `paymentMode` query | Pending certification |

## Required viewports
- Desktop representative viewport.
- Mobile 390 × 844.
- 200% root text scaling with no viewport horizontal overflow.
- Reduced-motion preference.
