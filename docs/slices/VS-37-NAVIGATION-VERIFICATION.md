# VS-37 Navigation Verification

| Journey | Runnable evidence | Expected result |
| --- | --- | --- |
| Package → sibling available date | `apps/web/e2e/package-details.spec.ts` — `available same-origin dates navigate and browser back returns safely` | Available sibling opens; browser back restores selected departure. |
| Package → date rail controls | `apps/web/e2e/package-details.spec.ts` — `Book now preserves Pay Full by default and shows the phone OTP design boundary` | Previous/Next controls are reachable 44px controls in the in-place rail. |
| Package → Book now | `apps/web/e2e/package-details.spec.ts` — `Book now preserves Pay Full by default and shows the phone OTP design boundary` | Occupancy and default `paymentMode=pay-full` reach planner. |
| Book now unauthenticated → phone OTP preview | same test | Phone field + Send Code visible; no-code-sent status appears; no SMS request is fabricated. |
| Authenticated planner → Add traveller | `apps/web/e2e/plan-journey.spec.ts` — `authenticated traveller step shows names first and adds travellers progressively` | Traveller names visible; Add traveller reveals name + DOB eligibility form. |
