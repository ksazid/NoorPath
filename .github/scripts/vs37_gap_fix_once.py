from pathlib import Path

page = Path("apps/web/app/packages/[departureId]/page.tsx")
text = page.read_text()
old = '''            <div className="package-conversion-content">
              <Journey details={details} />
              <PackageContent details={details} />
            </div>
          </div>
          <BookingCard
            details={details}
            selected={selected}
            paymentMode={paymentMode}
            onOccupancyChange={setOccupancy}
            onPaymentModeChange={setPaymentMode}
            onBookNow={() => setBookingOpen(true)}
          />
        </section>

        <div className="package-conversion-secondary">
          <TrustAndTerms details={details} />
          <section className="package-conversion-about">
            <h2>About this package</h2>
            <p>{details.summary}</p>
            <p>
              Published pricing, current room availability and payment commitments
              are visible before you start booking.
            </p>
          </section>
        </div>'''
new = '''            <div className="package-conversion-content">
              <Journey details={details} />
              <PackageContent details={details} />
            </div>
            <div className="package-conversion-secondary">
              <TrustAndTerms details={details} />
              <section className="package-conversion-about">
                <h2>About this package</h2>
                <p>{details.summary}</p>
                <p>
                  Published pricing, current room availability and payment commitments
                  are visible before you start booking.
                </p>
              </section>
            </div>
          </div>
          <BookingCard
            details={details}
            selected={selected}
            paymentMode={paymentMode}
            onOccupancyChange={setOccupancy}
            onPaymentModeChange={setPaymentMode}
            onBookNow={() => setBookingOpen(true)}
          />
        </section>'''
if old not in text:
    raise SystemExit("Package secondary layout block not found")
page.write_text(text.replace(old, new, 1))

css = Path("apps/web/app/packages/[departureId]/package-conversion.css")
css.write_text(
    css.read_text()
    + '''

/* VS-37 rendered-review correction: secondary facts continue in the left story. */
.package-conversion-primary > .package-conversion-secondary {
  margin-top: 22px;
}

.package-conversion-primary > .package-conversion-secondary .package-conversion-trust,
.package-conversion-primary > .package-conversion-secondary .package-conversion-about {
  width: 100%;
  box-sizing: border-box;
}
'''
)
