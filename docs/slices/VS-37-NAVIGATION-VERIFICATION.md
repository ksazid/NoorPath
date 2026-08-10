# VS-37 Navigation Verification

| Path                                                   | Expected outcome                                                             | Evidence                               |
| ------------------------------------------------------ | ---------------------------------------------------------------------------- | -------------------------------------- |
| `/packages/{departureId}` → Previous/Next travel dates | Date rail moves in place; URL remains hash-free                              | `apps/web/e2e/package-details.spec.ts` |
| `/packages/{departureId}` → available sibling date     | Navigates to sibling departure without a hash fragment; browser Back returns | `apps/web/e2e/package-details.spec.ts` |
| Package Details → Guests/Room/Payment                  | Local selection changes do not mutate URL hash                               | `apps/web/e2e/package-details.spec.ts` |
| Package Details → Book now                             | Opens OTP design sheet without route/hash mutation                           | `apps/web/e2e/package-details.spec.ts` |
| OTP preview → traveller step → + Add traveller         | Adds name-only traveller rows up to selected adult count; no persistence     | `apps/web/e2e/package-details.spec.ts` |
