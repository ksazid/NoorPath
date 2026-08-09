# VS-34 Implementation Checklist

## Specify / Design

- [x] Keep Operators authoritative for operator state, membership and permissions.
- [x] Keep Platform Administrator authority explicit and deny-by-default.
- [x] Preserve existing NoorPath protected-account visual language.
- [x] Exclude customer OTP provider configuration, Knowledge Pack and production deployment.

## Build

- [x] Add explicit operator lifecycle transition policy.
- [x] Add append-only operator lifecycle decision audit persistence.
- [x] Add forward-only Operators migration, generated metadata and snapshot update.
- [x] Add Platform Administrator operator summary/list/detail/state APIs.
- [x] Require reasons for rejection, suspension and deactivation.
- [x] Reject stale operator decisions using the existing optimistic version.
- [x] Replace `/admin` placeholder with operator command centre and history.
- [x] Keep publication review reachable from platform navigation.
- [x] Add persona-boundary and pending-to-approved cross-persona integration tests.
- [x] Add rendered admin desktop/mobile accessibility and interaction tests.

## Safety

- [x] Non-platform identities cannot list or mutate operator lifecycle state.
- [x] Pending, rejected, suspended and deactivated operators remain unable to use operator administration.
- [x] No OTP provider or production authentication claim is introduced.
- [x] No Knowledge Pack work is introduced.
- [x] Production deployment remains separately authorized.

## Verify / Close

- [ ] CI exact head passes.
- [ ] Slice Governance exact head passes.
- [ ] Rendered Slice Review exact head passes.
- [ ] Navigation Reachability exact head passes.
- [ ] Migration registry/model parity passes.
- [ ] Persona authorization integration tests pass.
- [ ] Existing Customer and Operator journey regression tests pass.
- [ ] `/admin` is keyboard/touch operable at desktop and 390px with no horizontal overflow.
- [ ] Standing Product Owner authorization is applied only to the final certified SHA.
- [ ] Production remains untouched.
