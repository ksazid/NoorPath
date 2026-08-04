# NoorPath Pull Request UX Checklist

Every feature PR must complete this checklist. A mandatory item may be skipped only with an explicit Product Owner justification.

## Flow and interaction

- [ ] The PR identifies the primary customer or staff task.
- [ ] The primary-flow interaction count is documented.
- [ ] The common path uses the fewest practical interactions.
- [ ] Routine actions are within three interactions after entering the relevant section, where practical.
- [ ] Each screen has one visually dominant primary action.
- [ ] Generic action labels have been replaced with outcome-based labels.
- [ ] Information is requested only when needed.
- [ ] Existing information is not requested again.
- [ ] Interrupted work can be resumed without restarting.

## Customer experience

- [ ] Customer navigation follows the journey rather than internal modules.
- [ ] Mobile behaviour was intentionally designed and tested.
- [ ] Price per traveller, total price, due-now amount, and remaining balance are clearly separated where relevant.
- [ ] WhatsApp Support and Request a Callback remain accessible where relevant.
- [ ] The next required action is visible and understandable.
- [ ] Authentication preserves package and reservation selections.

## Operator and administrator experience

- [ ] Staff navigation is under the correct Overview, Content, Operations, or Administration heading.
- [ ] Structured choices, defaults, templates, or cloning replace unnecessary free text.
- [ ] Operators edit content only and cannot alter protected presentation rules.
- [ ] Role and permission boundaries are enforced.
- [ ] Inaccessible modules are hidden.
- [ ] Privileged actions are auditable.
- [ ] Long forms use autosave or draft recovery where appropriate.

## Design system

- [ ] Approved NoorPath components are reused.
- [ ] Approved terminology is used.
- [ ] Approved icon mappings are used.
- [ ] No ad-hoc icon library or custom standard icon was introduced without approval.
- [ ] Header, footer, spacing, typography, colours, cards, and buttons follow approved standards.
- [ ] Package Details changes preserve the repository-approved design and fixed section order.
- [ ] Preview uses the actual customer-facing rendering.

## States and accessibility

- [ ] Loading, empty, unavailable, error, success, and retry states are implemented where applicable.
- [ ] Pending, action-required, and completed states are clearly differentiated.
- [ ] Keyboard navigation and focus behaviour are correct.
- [ ] Labels, touch targets, contrast, and screen-reader semantics were checked.
- [ ] Meaning does not depend only on colour.

## Performance and reliability

- [ ] The flow avoids unnecessary page loads and blocking transitions.
- [ ] Consequential actions provide immediate feedback.
- [ ] Duplicate submissions and repeated payments are prevented.
- [ ] Performance is acceptable on customer mobile devices.

## Evidence required in the PR

- [ ] Primary-flow interaction count.
- [ ] Desktop screenshot or recording.
- [ ] Mobile screenshot or recording.
- [ ] Accessibility evidence.
- [ ] Validation and test evidence.
- [ ] Explanation of any intentionally unmet product principle.
