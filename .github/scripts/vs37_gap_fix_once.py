from pathlib import Path
import textwrap

page = Path("apps/web/app/packages/[departureId]/page.tsx")
text = page.read_text()
experience_start = text.index("function PackageExperience")
secondary_start = text.index(
    '\n        <div className="package-conversion-secondary">', experience_start
)
secondary_end = text.index("\n      </main>", secondary_start)
secondary_block = text[secondary_start:secondary_end]
text_without_secondary = text[:secondary_start] + text[secondary_end:]
primary_close = text_without_secondary.index(
    "\n          </div>\n          <BookingCard", experience_start
)
secondary_inside = "\n" + textwrap.indent(secondary_block.strip("\n"), "    ")
text = (
    text_without_secondary[:primary_close]
    + secondary_inside
    + text_without_secondary[primary_close:]
)
page.write_text(text)

css = Path("apps/web/app/packages/[departureId]/package-conversion.css")
css_text = css.read_text()
marker = "/* VS-37 rendered-review correction: secondary facts continue in the left story. */"
if marker not in css_text:
    css_text += '''

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
css.write_text(css_text)
