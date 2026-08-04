# VS-19 — Navigation Verification Matrix

This matrix is mandatory under `docs/06-engineering/NAVIGATION-VERIFICATION-GATE.md`.

Direct URL tests do not prove reachability. VS-19 therefore verifies changed customer header items, footer items, breadcrumbs, cards, support entries and transactional exits by clicking the real source controls. Existing slice tests remain authoritative for unchanged domain transitions and account-isolation rules.

## Public navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Public | Shared customer header | NoorPath brand | `/` | Public | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Clicked from support and transactional contexts. |
| Public | Landing header | `Packages` | `/#packages` | Public | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Clicked from the real desktop header. |
| Public | Landing header | `How It Works` | `/#plan-ahead` | Public | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Clicked from the real desktop header. |
| Public | Landing header | `Talk to Us` | `/support` | Public | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Destination renders approved safe support guidance. |
| Public | Landing header | `My Journey` | `/journeys` | Public/synthetic customer fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Public click-through is verified; real Auth0 return-to remains separately blocked below. |
| Public | Discovery result | `View package` | `/packages/{departureId}` | Public | VERIFIED | `e2e/customer-shell-navigation.spec.ts`; `e2e/customer-states.spec.ts` | Clicked from a mocked authoritative published result. |
| Public | Package Details | Shared header and full footer | Public destinations | Public | VERIFIED | `e2e/customer-shell-navigation.spec.ts`; `e2e/package-details.spec.ts` | Existing fixed Package Details content and hierarchy remain unchanged. |
| Public | Package Details | `Plan this journey` | `/packages/{departureId}/plan` | Public/customer | VERIFIED | `e2e/customer-shell-navigation.spec.ts`; `e2e/plan-journey.spec.ts` | Shell composition does not alter quote or hold behavior. |
| Public | Full footer | Package, journey, support and legal links | Approved destinations | Public | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Privacy and Terms are clicked; other destinations have exact href checks and route coverage. |

## Authenticated customer navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Customer | Authenticated header | `Packages` | `/#packages` | Synthetic customer fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Clicked from My Journey. |
| Customer | Authenticated header | `My Journey` | `/journeys` | Synthetic customer fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Active state is verified. |
| Customer | Authenticated header | `Help` | `/support` | Synthetic customer fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Clicked from My Journey. |
| Customer | Authenticated header | `Talk to Us` | `/support` | Synthetic customer fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Clicked from My Journey. |
| Customer | Authenticated header | `Profile` | `/account` | Synthetic authorized customer fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Clicked and authorized account heading verified. |
| Customer | My Journey list | `View journey` | `/bookings/{bookingId}/journey` | Synthetic booking owner fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts`; `e2e/my-journey.spec.ts` | Real list-card click is used. |
| Customer | Journey detail | `Manage documents` | `/bookings/{bookingId}/documents` | Synthetic booking owner fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts`; existing document rendered/integration suites | Transactional shell is verified after the click. |
| Customer | Journey detail | `View visa status` | `/bookings/{bookingId}/visa` | Synthetic booking owner fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts`; existing visa rendered/integration suites | Authenticated shell is verified after the click. |
| Customer | Journey detail | `Review cancellation options` | `#cancellation` | Synthetic booking owner fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts`; `e2e/cancellation-refunds.spec.ts` | PR #77 reachability remains intact. |
| Customer | Journey detail | Support action | Safe booking-reference email/support context | Synthetic booking owner fixture | VERIFIED | `e2e/my-journey.spec.ts`; VS-19 changed-file review | Existing support link contains safe references only; shell adds no customer identifiers. |
| Customer | Journey breadcrumb | `My Journey` | `/journeys` | Synthetic booking owner fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Clicked from the real breadcrumb. |
| Customer | Foreign-account route/API | Booking-owned resources | Safe not-found | Authenticated non-owner fixture | VERIFIED | existing journey, document, visa and cancellation integration suites; PR #77 evidence | VS-19 changes no API, persistence or authorization code. |
| Customer | Authenticated header | Any staff/admin entry | No destination rendered | Synthetic customer fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Operator and administrator links are absent on desktop and mobile. |

## Transactional navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Transactional | Sign-in entry | Brand/home and `Talk to Us` | `/` and `/support` | Public/customer fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Reduced-distraction header is verified; phone OTP provider remains outside this slice. |
| Transactional | Package planning and inventory hold | Existing plan, hold and booking controls | Adjacent approved booking step | Synthetic customer fixture | VERIFIED | `e2e/plan-journey.spec.ts`; VS-19 route classification | Existing controls and state preservation are unchanged; shared shell adds only chrome. |
| Transactional | Booking/payment | Existing breadcrumb/help controls | Package plan or support | Synthetic customer fixture | VERIFIED | existing VS-09 rendered/integration suites; VS-19 route classification and compact-footer test | No payment handlers or financial state code changed. |
| Transactional | Confirmation | Existing continue action | Confirmed booking/My Journey route | Synthetic customer fixture | VERIFIED | existing VS-10 rendered/integration suites; VS-19 route classification | Confirmation behavior remains page-owned. |
| Transactional | Traveller details | Existing back/continue controls | Adjacent approved step | Synthetic customer fixture | VERIFIED | existing VS-08/VS-09 rendered suites; changed-file review | Shell composition does not own or mutate traveller selection. |
| Transactional | Document upload | Journey-owned document controls and shell support | Booking-owned document/journey routes | Synthetic booking owner fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts`; existing document rendered/integration suites | Documents route is reached from journey detail and renders the transactional shell. |
| Transactional | Compact footer | Support, Privacy and Terms | `/support`, `/privacy`, `/terms` | Applicable fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | All three destinations are checked; Privacy and Terms are clicked. |
| Transactional | Header | Promotional or staff navigation | No destination rendered | Applicable fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | Full customer navigation is absent. |

## Mobile and responsive navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Mobile public | Native details trigger | Open and close customer menu | Same page/menu state | Public | VERIFIED | Mobile WebKit in `e2e/customer-shell-navigation.spec.ts` | `open` state is verified; no modal focus trap is required for the non-modal details pattern. |
| Mobile public | Mobile menu | Packages, How It Works, Talk to Us and My Journey | Approved destinations | Public | VERIFIED | Mobile WebKit in `e2e/customer-shell-navigation.spec.ts` | Labels, visibility and support click-through are verified. |
| Mobile customer | Native details trigger/menu | Packages, My Journey, Help, Talk to Us and Profile | Approved destinations | Synthetic customer fixture | VERIFIED | Mobile WebKit in `e2e/customer-shell-navigation.spec.ts` | Staff/admin links are absent. |
| Mobile shared | Breadcrumbs and page-owned back controls | Parent destination | Correct customer route | Applicable fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts`; existing mobile journey/package tests | Journey breadcrumb click is verified; existing route controls remain unchanged. |
| Mobile shared | Footer/support links | Approved destinations | Valid route/action | Applicable fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | 200% text scaling, minimum targets and horizontal reflow are checked. |
| Responsive shared | Orientation/viewport change | Native details state and layout | Usable current route | Applicable fixture | VERIFIED | desktop/mobile projects plus 390px and 200% checks | Native details remains operable without a blocking overlay. |

## Staff exclusion

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Operator | `/operator` | Existing protected operator shell | Existing operator access boundary | Unauthenticated fixture | VERIFIED | `e2e/customer-shell-navigation.spec.ts` | No `data-customer-shell` wrapper is rendered. |
| Admin/platform | `/admin` and `/platform/*` | Existing protected staff routes | Existing role boundary | Applicable staff identity | VERIFIED | route-classification contract and changed-file review | Staff routes are explicitly excluded from the customer adapter. |

## Identity-restricted production verification

| Route/path | Required identity/configuration | What can be verified now | Result | Follow-up verification |
| --- | --- | --- | --- | --- |
| Public My Journey/deep booking link → sign-in → exact customer route | `AUTH0_DOMAIN`, `AUTH0_CLIENT_ID`, `AUTH0_CLIENT_SECRET`, `AUTH0_SECRET` and real booking-owner identity | Local/synthetic route, safe return-url validation and customer-shell contract | BLOCKED_IDENTITY | Configure Auth0 and verify an unauthenticated real booking-owner deep link returns to the exact requested route before real production release. |
| Authenticated customer header/profile with real session | Configured Auth0 customer application and real test customer identity | Synthetic authorized account fixture and customer navigation | BLOCKED_IDENTITY | Verify real login, session, customer navigation, logout and profile destination before real production release. |

## Completion rule

Before VS-19 receives `certify`:

- every result row must be `VERIFIED`, `BLOCKED_IDENTITY` or `NOT_APPLICABLE`;
- no failed result may remain;
- desktop Chromium and mobile WebKit evidence must click from real source controls;
- direct route tests may supplement but never replace source-to-destination verification;
- foreign-account isolation must be retained;
- all `BLOCKED_IDENTITY` rows must remain in the PR and real production checklist;
- evidence must belong to the exact unchanged implementation SHA.
