# VS-37 Design Notes

## Design read
Package Details is a purchase-decision dossier. The main column tells the journey story continuously; the booking rail handles choices and commercial disclosure.

## Spatial thesis
- Lead: package imagery + verified operator/stay facts.
- Immediately below: itinerary, inclusions/exclusions, status/terms and package narrative.
- Support rail: available dates → guests → room → payment mode → payment schedule → full price breakdown → Book now.
- Mobile: hero first, booking rail second, then itinerary/content; the sticky bottom summary keeps the primary action continuously reachable.

## Truth boundaries
- Airline is `To be confirmed` until supplier data exists.
- Discount UI is structurally present but explicitly `0% / ₹0` with `No published discount` until governed discount data exists.
- Tax copy promises disclosure before payment, not a fabricated tax amount.
- OTP form is a preview while SMS is not configured and never claims a code was sent.

## Interaction
- Date navigation offers visible previous/next buttons plus native horizontal scrolling.
- No decorative motion. Press feedback stays short and reduced-motion safe.
- Add traveller uses progressive disclosure to keep the authenticated step name-first without weakening adult eligibility validation.
