# VS-19 — Navigation Verification Matrix

This matrix is mandatory under `docs/06-engineering/NAVIGATION-VERIFICATION-GATE.md`.

Direct URL tests do not prove reachability. Every changed customer header item, footer item, breadcrumb, card link, support entry and transactional back/exit control must be verified by clicking the real source control on desktop Chromium and mobile WebKit where applicable.

## Public navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Public | Landing header | Brand/home control | `/` | Public | PENDING | `e2e/customer-shell-navigation.spec.ts` | Verify desktop and mobile. |
| Public | Landing/discovery header | `Packages` | Approved package discovery route | Public | PENDING | `e2e/customer-shell-navigation.spec.ts` | Destination must be repository truth, not invented. |
| Public | Landing/discovery header | `How It Works` | Approved How It Works destination | Public | PENDING | `e2e/customer-shell-navigation.spec.ts` | Verify active state where applicable. |
| Public | Landing/discovery header | `Talk to Us` | Approved support destination | Public | PENDING | `e2e/customer-shell-navigation.spec.ts` | Must expose WhatsApp/callback only where configured. |
| Public | Landing/discovery header | `My Journey` | Public journey entry/auth boundary | Public | PENDING | `e2e/customer-shell-navigation.spec.ts` | Real Auth0 return-to may remain BLOCKED_IDENTITY. |
| Public | Discovery result | Package card/action | Package Details | Public | PENDING | Existing discovery + VS-19 Playwright | Verify card and primary action. |
| Public | Package Details | Header navigation | Public destinations | Public | PENDING | `e2e/customer-shell-navigation.spec.ts` | Preserve fixed package section order. |
| Public | Package Details | Reservation action | Existing reservation/booking entry | Public/customer | PENDING | Existing package/booking tests | Shell must not change commercial state. |
| Public | Full footer | Brand, package, support and legal links | Approved destinations | Public | PENDING | `e2e/customer-shell-navigation.spec.ts` | Verify all visible links, desktop/mobile. |

## Authenticated customer navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Customer | Authenticated header | `Packages` | Approved package discovery route | Authenticated customer fixture | PENDING | `e2e/customer-shell-navigation.spec.ts` | Staff routes must be absent. |
| Customer | Authenticated header | `My Journey` | My Journey list | Authenticated customer fixture | PENDING | `e2e/customer-shell-navigation.spec.ts` | Verify active state. |
| Customer | Authenticated header | `Help` | Approved help destination | Authenticated customer fixture | PENDING | `e2e/customer-shell-navigation.spec.ts` | No placeholder dead end. |
| Customer | Authenticated header | `Talk to Us` | Approved support destination | Authenticated customer fixture | PENDING | `e2e/customer-shell-navigation.spec.ts` | Safe support context only. |
| Customer | Authenticated header | `Profile` | Customer account/profile | Authenticated customer fixture | PENDING | `e2e/customer-shell-navigation.spec.ts` | No staff account route. |
| Customer | My Journey list | Journey card | Journey detail | Authenticated booking owner fixture | PENDING | Existing My Journey + VS-19 Playwright | Verify real card click. |
| Customer | Journey detail | Documents control | Booking-owned documents destination | Authenticated booking owner fixture | PENDING | VS-19 Playwright | Preserve account isolation. |
| Customer | Journey detail | Visa control | Booking-owned visa destination/section | Authenticated booking owner fixture | PENDING | VS-19 Playwright | Customer-safe status only. |
| Customer | Journey detail | Cancellation/refund control | Booking-owned cancellation destination/section | Authenticated booking owner fixture | PENDING | VS-16 + VS-19 Playwright | Preserve PR #77 reachability. |
| Customer | Journey detail | Support control | Approved support destination | Authenticated booking owner fixture | PENDING | VS-19 Playwright | Safe booking reference only. |
| Customer | Breadcrumb/back control | Parent customer route | Correct list/parent destination | Authenticated customer fixture | PENDING | VS-19 Playwright | Verify desktop/mobile. |
| Customer | Foreign-account route | Direct URL/API following visible context | Safe not-found | Authenticated non-owner fixture | PENDING | Integration/Playwright evidence | No existence leakage. |

## Transactional navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Transactional | OTP/auth entry | Brand/back/exit control | Safe approved destination | Public/customer fixture | PENDING | VS-19 Playwright | Preserve return-to state. |
| Transactional | Reservation/booking | Back control | Prior approved booking step/package | Customer fixture | PENDING | Existing booking + VS-19 Playwright | No selection loss. |
| Transactional | Payment | Back/help control | Safe payment context/support | Customer fixture | PENDING | Existing payment + VS-19 Playwright | Must not duplicate financial effects. |
| Transactional | Confirmation | Continue control | My Journey/confirmed booking destination | Customer fixture | PENDING | Existing confirmation + VS-19 Playwright | Preserve booking reference. |
| Transactional | Traveller details | Back/continue controls | Adjacent approved step | Customer fixture | PENDING | VS-19 Playwright | No domain-state change from shell. |
| Transactional | Document upload | Back/continue/support controls | Booking-owned document/journey routes | Booking owner fixture | PENDING | Existing documents + VS-19 Playwright | Preserve document authorization. |
| Transactional | Compact footer | Legal/support links | Approved destinations | Applicable identity | PENDING | VS-19 Playwright | Reduced-distraction footer only. |

## Mobile and responsive navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Mobile public | Header trigger | Open/close mobile navigation | Same page/menu state | Public | PENDING | Mobile WebKit | Verify expanded state, Escape/focus return where applicable. |
| Mobile public | Mobile menu | Every public navigation item | Approved destination | Public | PENDING | Mobile WebKit | Verify target size and no clipping. |
| Mobile customer | Header trigger | Open/close authenticated navigation | Same page/menu state | Authenticated fixture | PENDING | Mobile WebKit | Staff routes absent. |
| Mobile customer | Mobile menu | Every customer navigation item | Approved destination | Authenticated fixture | PENDING | Mobile WebKit | Verify active state and focus. |
| Mobile shared | Breadcrumb/back controls | Parent/previous destination | Correct customer route | Applicable identity | PENDING | Mobile WebKit | No dead end after refresh. |
| Mobile shared | Footer/support links | Approved destinations | Valid route/action | Applicable identity | PENDING | Mobile WebKit | Verify reflow at 200% text. |

## Identity-restricted production verification

| Route/path | Required identity/configuration | What can be verified now | Result | Follow-up verification |
| --- | --- | --- | --- | --- |
| Public My Journey/deep booking link → sign-in → exact customer route | `AUTH0_DOMAIN`, `AUTH0_CLIENT_ID`, `AUTH0_CLIENT_SECRET`, `AUTH0_SECRET` and real booking-owner identity | Local/synthetic route and return-to contract only | BLOCKED_IDENTITY | Configure Auth0 and verify an unauthenticated real booking-owner deep link returns to the exact requested customer route before real production release. |
| Authenticated customer header/profile with real session | Configured Auth0 customer application and real test customer identity | Synthetic/test fixture shell behavior only | BLOCKED_IDENTITY | Verify real login, session, customer navigation, logout and profile destination before real production release. |

## Completion rule

Before VS-19 receives `certify`:

- every `PENDING` row must become `VERIFIED`, `BLOCKED_IDENTITY` or `NOT_APPLICABLE`;
- no `FAILED` row may remain;
- desktop Chromium and mobile WebKit evidence must click from real source controls;
- direct route tests may supplement but never replace source-to-destination verification;
- foreign-account isolation must be retained;
- all `BLOCKED_IDENTITY` rows must remain in the PR and real production checklist;
- evidence must belong to the exact unchanged implementation SHA.
