# VS-34 Navigation Verification

## Platform Administrator

1. Open `/admin` with an authorized Platform Administrator identity.
2. Confirm the `Platform operations` command centre loads.
3. Confirm Overview and Operators remain reachable within the page.
4. Confirm `Publication reviews` reaches `/platform/publications`.
5. Confirm a pending operator appears before non-pending operators.
6. Apply an allowed decision and confirm the queue reloads to the new authoritative state.
7. Open `View history` and confirm the transition evidence is visible.
8. Confirm stale state feedback reloads the latest operator state rather than overwriting it.

## Demo persona boundaries

- Customer demo identity: `/api/v1/account/access` allowed; operator/platform access denied.
- Approved operator demo identity: account/operator access allowed; platform access denied.
- Platform Administrator demo identity: account/platform access allowed; operator access denied unless separately provisioned.
- Pending operator demo identity: operator access denied before approval and allowed after Platform Administrator approval.

## Responsive / accessibility verification

- Run the VS-34 rendered test at desktop width and 390 x 844.
- Verify no horizontal overflow.
- Verify interactive targets meet the 44px minimum.
- Verify WCAG 2.2 AA automated checks are clean.
- Verify every decision input has a visible label and history disclosure exposes its expanded state.

## Explicitly not certified by VS-34

- Customer phone OTP provider delivery/configuration.
- Knowledge Pack.
- Production deployment.
