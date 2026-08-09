# VS-31 Implementation Checklist

## Shell

- [x] One operator wordmark/header treatment is used across operator workspaces.
- [x] Operator business context and signed-in member account menu remain distinct.
- [x] Desktop operator navigation is grouped and route-aware.
- [x] Mobile operator navigation exposes the same destinations.
- [x] Shared content width, page heading rhythm and focus behavior are aligned.

## Legacy operator surfaces

- [x] Package quick start / clone flow is embedded in shared operator chrome.
- [x] New and existing departure authoring is embedded in shared operator chrome.
- [x] Customer preview is embedded in shared operator chrome.
- [x] Publication review is embedded in shared operator chrome.
- [x] Competing legacy sidebar/brand treatments are suppressed only inside the shared operator embed.

## Safety

- [x] Operator access remains fail-closed and server authorization is unchanged.
- [x] No package, pricing, inventory, booking, payment or fulfilment business rules changed.
- [x] No new dependency or icon family added.
- [x] Customer shell/footer is unchanged.

## Verification

- [ ] CI exact head passes.
- [ ] Slice Governance exact head passes.
- [ ] Rendered Slice Review exact head passes.
- [ ] Navigation Reachability exact head passes.
- [ ] Desktop representative routes retain one shared header/sidebar.
- [ ] 390px operator route has no horizontal overflow and exposes mobile navigation.
- [ ] Standing Product Owner authorization is applied only to the final certified SHA.
- [ ] Production deployment remains separately authorized.
