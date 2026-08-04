import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const bookingId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const departureId = "3c9d522a-9481-4b79-9486-64cf997bfe31";

const journeyList = {
  items: [
    {
      bookingId,
      bookingReference: "NP-20260801-JOURNEY",
      travellerCount: 2,
      currency: "GBP",
      total: 6400,
      confirmedAtUtc: "2026-08-01T07:00:00Z",
    },
  ],
};

const journey = {
  bookingId,
  bookingReference: "NP-20260801-JOURNEY",
  state: "Confirmed",
  occupancy: "Double",
  confirmedAtUtc: "2026-08-01T07:00:00Z",
  journey: {
    packageName: "The NoorPath Signature Umrah",
    origin: "London Heathrow",
    departureDate: "2026-11-08",
    returnDate: "2026-11-20",
    makkahHotelName: "Jabal Omar Marriott",
    makkahNights: 7,
    madinahHotelName: "Pullman Zamzam Madina",
    madinahNights: 5,
    travelRouteSummary: "London to Jeddah · Madinah to London",
  },
  travellers: [
    { travellerId: "traveller-1", fullName: "Amina Rahman" },
    { travellerId: "traveller-2", fullName: "Yusuf Rahman" },
  ],
  family: null,
  commercial: { currency: "GBP", total: 6400, paid: 1600, remaining: 4800 },
  payment: {
    state: "Succeeded",
    instalments: [
      {
        sequence: 1,
        dueDate: "2026-09-15",
        amount: 2400,
        status: "Scheduled",
      },
      {
        sequence: 2,
        dueDate: "2026-10-15",
        amount: 2400,
        status: "Scheduled",
      },
    ],
  },
  readiness: { documents: "ActionRequired", visa: "InProgress" },
  support: {
    bookingReference: "NP-20260801-JOURNEY",
    correlationId: "safe-correlation-id",
  },
};

const publishedDetail = {
  departureId,
  operator: {
    id: "operator-noor",
    displayName: "Noor International Tours & Travels",
  },
  packageName: "Browser Verified Journey",
  summary: "A published journey backed by authoritative NoorPath facts.",
  origin: "Delhi (DEL)",
  departureDate: "2026-10-10",
  returnDate: "2026-10-22",
  durationNights: 12,
  makkah: {
    hotelName: "Makkah Hotel",
    classification: "4 star",
    distanceDisclosure: "850 m from Masjid al-Haram",
    nights: 6,
    confirmationState: "confirmed",
  },
  madinah: {
    hotelName: "Madinah Hotel",
    classification: "4 star",
    distanceDisclosure: "450 m from Al-Masjid an-Nabawi",
    nights: 5,
    confirmationState: "pending",
  },
  travel: {
    routeSummary: "Delhi → Jeddah → Makkah → Madinah",
    details: "Final carrier and flight timing remain pending.",
    confirmationState: "pending",
  },
  inclusions: ["Return flights", "Breakfast", "Visa support"],
  exclusions: ["Personal expenses"],
  pricing: {
    currency: "INR",
    occupancies: [
      {
        occupancy: "double",
        amount: 110000,
        availableQuantity: 10,
        status: "available",
      },
      {
        occupancy: "triple",
        amount: 100000,
        availableQuantity: 0,
        status: "unavailable",
      },
      {
        occupancy: "quad",
        amount: 90000,
        availableQuantity: 6,
        status: "available",
      },
    ],
  },
};

const publishedDiscovery = {
  items: [
    {
      departureId,
      operator: publishedDetail.operator,
      packageName: publishedDetail.packageName,
      summary: publishedDetail.summary,
      origin: publishedDetail.origin,
      departureDate: publishedDetail.departureDate,
      returnDate: publishedDetail.returnDate,
      durationNights: publishedDetail.durationNights,
      makkah: publishedDetail.makkah,
      madinah: publishedDetail.madinah,
      travelConfirmationState: "pending",
      inclusionHighlights: ["Return flights", "Breakfast"],
      headlinePrice: {
        amount: 90000,
        currency: "INR",
        occupancy: "quad",
      },
      availability: {
        status: "available",
        occupancies: [
          { occupancy: "double", availableQuantity: 10 },
          { occupancy: "triple", availableQuantity: 8 },
          { occupancy: "quad", availableQuantity: 6 },
        ],
      },
    },
  ],
};

async function mockCustomerJourneys(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/journeys", (route) =>
    route.fulfill({ json: journeyList }),
  );
  await page.route(`**/api/v1/journeys/${bookingId}`, (route) =>
    route.fulfill({ json: journey }),
  );
}

async function mockPublicPackages(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/departures", (route) =>
    route.fulfill({ json: publishedDiscovery }),
  );
  await page.route(`**/api/v1/departures/${departureId}`, (route) =>
    route.fulfill({ json: publishedDetail }),
  );
}

function desktopNavigation(page: import("@playwright/test").Page) {
  return page
    .locator(".np-customer-navigation--desktop")
    .getByRole("navigation", { name: "Customer navigation" });
}

test("public shell exposes valid discovery, package, support and legal destinations", async ({
  page,
}, testInfo) => {
  await mockCustomerJourneys(page);
  await mockPublicPackages(page);
  await page.goto("/");

  const shell = page.locator('[data-customer-shell="public"]');
  await expect(shell).toBeVisible();

  const navigation = desktopNavigation(page);
  for (const label of ["Packages", "How It Works", "Talk to Us", "My Journey"]) {
    await expect(
      navigation.getByRole("link", { name: label, exact: true }),
    ).toBeVisible();
  }

  await navigation.getByRole("link", { name: "Packages" }).click();
  await expect(page).toHaveURL(/\/#packages$/);

  await navigation.getByRole("link", { name: "How It Works" }).click();
  await expect(page).toHaveURL(/\/#plan-ahead$/);

  await navigation.getByRole("link", { name: "My Journey" }).click();
  await expect(page).toHaveURL(/\/journeys$/);
  await expect(page.getByRole("heading", { name: "My Journey" })).toBeVisible();

  await page.goto("/");
  await expect(
    page.getByRole("heading", { name: "Browser Verified Journey" }),
  ).toBeVisible();
  await page.getByRole("link", { name: /View package/ }).click();
  await expect(page).toHaveURL(new RegExp(`/packages/${departureId}$`));
  await expect(
    page.getByRole("heading", {
      name: "Noor International Tours & Travels",
    }),
  ).toBeVisible();
  await expect(page.locator('[data-customer-shell="public"]')).toBeVisible();
  await expect(page.locator(".np-customer-footer:visible")).toBeVisible();

  await page.getByRole("link", { name: "Plan this journey" }).click();
  await expect(page).toHaveURL(new RegExp(`/packages/${departureId}/plan$`));
  await expect(page.locator('[data-customer-shell="public"]')).toBeVisible();

  await page.goto("/");
  await desktopNavigation(page).getByRole("link", { name: "Talk to Us" }).click();
  await expect(page).toHaveURL(/\/support$/);
  await expect(
    page.getByRole("heading", { name: "Talk to NoorPath" }),
  ).toBeVisible();

  await page.getByRole("link", { name: "NoorPath home" }).first().click();
  await expect(page).toHaveURL(/\/$/);

  const footer = page.locator(".np-customer-footer:visible");
  await expect(footer.getByRole("link", { name: "Privacy" })).toHaveAttribute(
    "href",
    "/privacy",
  );
  await expect(footer.getByRole("link", { name: "Terms" })).toHaveAttribute(
    "href",
    "/terms",
  );
  await expect(
    footer.getByRole("link", { name: "Family Travellers" }),
  ).toHaveAttribute("href", "/account/family");

  await footer.getByRole("link", { name: "Privacy" }).click();
  await expect(page).toHaveURL(/\/privacy$/);
  await expect(
    page.getByRole("heading", {
      name: "How NoorPath handles journey information",
    }),
  ).toBeVisible();

  await page
    .locator(".np-customer-footer:visible")
    .getByRole("link", { name: "Terms" })
    .click();
  await expect(page).toHaveURL(/\/terms$/);
  await expect(
    page.getByRole("heading", {
      name: "Understand every commitment before booking",
    }),
  ).toBeVisible();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);

  await testInfo.attach("vs19-public-shell", {
    body: await page.screenshot({ fullPage: true }),
    contentType: "image/png",
  });
});

test("authenticated shell preserves header, journey and booking-owned reachability", async ({
  page,
}, testInfo) => {
  await mockCustomerJourneys(page);
  await mockPublicPackages(page);
  await page.route("**/api/v1/account/access", (route) =>
    route.fulfill({ status: 200, json: {} }),
  );
  await page.goto("/journeys");

  const shell = page.locator('[data-customer-shell="authenticated"]');
  await expect(shell).toBeVisible();
  const navigation = desktopNavigation(page);

  for (const label of [
    "Packages",
    "My Journey",
    "Help",
    "Talk to Us",
    "Profile",
  ]) {
    await expect(
      navigation.getByRole("link", { name: label, exact: true }),
    ).toBeVisible();
  }
  await expect(
    navigation.getByRole("link", { name: "My Journey" }),
  ).toHaveAttribute("aria-current", "page");
  await expect(
    navigation.getByRole("link", { name: /operator|admin/i }),
  ).toHaveCount(0);

  await navigation.getByRole("link", { name: "Help", exact: true }).click();
  await expect(page).toHaveURL(/\/support$/);

  await page.goto("/journeys");
  await desktopNavigation(page)
    .getByRole("link", { name: "Talk to Us", exact: true })
    .click();
  await expect(page).toHaveURL(/\/support$/);

  await page.goto("/journeys");
  await desktopNavigation(page)
    .getByRole("link", { name: "Profile", exact: true })
    .click();
  await expect(page).toHaveURL(/\/account$/);
  await expect(page.getByRole("heading", { name: "My NoorPath" })).toBeVisible();

  await page.goto("/journeys");
  await desktopNavigation(page)
    .getByRole("link", { name: "Packages", exact: true })
    .click();
  await expect(page).toHaveURL(/\/#packages$/);

  await page.goto("/journeys");
  await page.getByRole("link", { name: "View journey" }).click();
  await expect(page).toHaveURL(
    new RegExp(`/bookings/${bookingId}/journey$`),
  );
  await expect(
    page.getByRole("heading", { name: journey.journey.packageName }),
  ).toBeVisible();

  await page.getByRole("link", { name: "Review cancellation options" }).click();
  await expect(page).toHaveURL(/#cancellation$/);
  await expect(page.locator("#cancellation")).toBeVisible();

  const documentsLink = page.getByRole("link", { name: "Manage documents" });
  await expect(documentsLink).toHaveAttribute(
    "href",
    `/bookings/${bookingId}/documents`,
  );
  await documentsLink.click();
  await expect(page).toHaveURL(
    new RegExp(`/bookings/${bookingId}/documents$`),
  );
  await expect(
    page.locator('[data-customer-shell="transactional"]'),
  ).toBeVisible();

  await page.goto(`/bookings/${bookingId}/journey`);
  await page.getByRole("link", { name: "View visa status" }).click();
  await expect(page).toHaveURL(new RegExp(`/bookings/${bookingId}/visa$`));
  await expect(
    page.locator('[data-customer-shell="authenticated"]'),
  ).toBeVisible();

  await page.goto(`/bookings/${bookingId}/journey`);
  await page
    .getByRole("navigation", { name: "Breadcrumb" })
    .getByRole("link")
    .click();
  await expect(page).toHaveURL(/\/journeys$/);

  await expectNoHorizontalOverflow(page);
  await testInfo.attach("vs19-authenticated-shell", {
    body: await page.screenshot({ fullPage: true }),
    contentType: "image/png",
  });
});

test("transactional shell provides reduced-distraction support and legal exits", async ({
  page,
}, testInfo) => {
  await page.goto("/auth/sign-in?returnUrl=%2Fjourneys");

  const shell = page.locator('[data-customer-shell="transactional"]');
  await expect(shell).toBeVisible();
  await expect(page.getByText("Secure NoorPath journey").first()).toBeVisible();
  await expect(
    shell.getByRole("navigation", { name: "Customer navigation" }),
  ).toHaveCount(0);

  const compactFooter = shell.locator(".np-customer-footer--compact");
  await expect(compactFooter).toBeVisible();
  await expect(
    compactFooter.getByRole("link", { name: "Support" }),
  ).toHaveAttribute("href", "/support");
  await expect(
    compactFooter.getByRole("link", { name: "Privacy" }),
  ).toHaveAttribute("href", "/privacy");
  await expect(
    compactFooter.getByRole("link", { name: "Terms" }),
  ).toHaveAttribute("href", "/terms");

  await shell.getByRole("link", { name: "NoorPath home" }).first().click();
  await expect(page).toHaveURL(/\/$/);

  await page.goto("/auth/sign-in?returnUrl=%2Fjourneys");
  await page
    .locator(".np-customer-footer--compact")
    .getByRole("link", { name: "Privacy" })
    .click();
  await expect(page).toHaveURL(/\/privacy$/);

  await page.goto("/auth/sign-in?returnUrl=%2Fjourneys");
  await page
    .locator(".np-customer-footer--compact")
    .getByRole("link", { name: "Terms" })
    .click();
  await expect(page).toHaveURL(/\/terms$/);

  await page.goto("/auth/sign-in?returnUrl=%2Fjourneys");
  await page.getByRole("link", { name: "Talk to Us" }).click();
  await expect(page).toHaveURL(/\/support$/);

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);

  await testInfo.attach("vs19-transactional-shell", {
    body: await page.screenshot({ fullPage: true }),
    contentType: "image/png",
  });
});

test("mobile public and authenticated menus reflow without exposing staff routes", async ({
  page,
}, testInfo) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.goto("/");

  const publicMenu = page.locator(".np-customer-menu");
  await publicMenu.getByText("Menu", { exact: true }).click();
  await expect(publicMenu).toHaveAttribute("open", "");
  const publicPanel = publicMenu.locator(".np-customer-menu__panel");
  for (const label of [
    "Packages",
    "How It Works",
    "Talk to Us",
    "My Journey",
  ]) {
    await expect(
      publicPanel.getByRole("link", { name: label, exact: true }),
    ).toBeVisible();
  }
  await publicMenu.getByText("Menu", { exact: true }).click();
  await expect(publicMenu).not.toHaveAttribute("open", "");
  await publicMenu.getByText("Menu", { exact: true }).click();
  await publicPanel.getByRole("link", { name: "Talk to Us" }).click();
  await expect(page).toHaveURL(/\/support$/);

  await mockCustomerJourneys(page);
  await page.goto("/journeys");
  const customerMenu = page.locator(".np-customer-menu");
  await customerMenu.getByText("Menu", { exact: true }).click();
  const customerPanel = customerMenu.locator(".np-customer-menu__panel");
  for (const label of [
    "Packages",
    "My Journey",
    "Help",
    "Talk to Us",
    "Profile",
  ]) {
    await expect(
      customerPanel.getByRole("link", { name: label, exact: true }),
    ).toBeVisible();
  }
  await expect(
    customerPanel.getByRole("link", { name: /operator|admin/i }),
  ).toHaveCount(0);

  await page.evaluate(() => {
    document.documentElement.style.fontSize = "200%";
  });
  await expectNoHorizontalOverflow(page);
  await expectMinimumTargets(page);

  await testInfo.attach("vs19-mobile-shell-200-percent", {
    body: await page.screenshot({ fullPage: true }),
    contentType: "image/png",
  });
});

test("staff routes remain outside customer shell adoption", async ({ page }) => {
  await page.route("**/api/v1/operator/access", (route) =>
    route.fulfill({ status: 401, json: {} }),
  );
  await page.goto("/operator");
  await expect(page.locator("[data-customer-shell]")).toHaveCount(0);
  await expect(
    page.getByRole("heading", { name: "Sign in to continue" }),
  ).toBeVisible();
});
