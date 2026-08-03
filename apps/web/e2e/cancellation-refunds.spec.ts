import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const bookingId = "16161616-1616-4161-8161-161616161616";
const cancellationId = "26262626-2626-4262-8262-262626262626";
const refundId = "36363636-3636-4363-8363-363636363636";

const journey = {
  bookingId,
  bookingReference: "NP-20260803-CANCEL",
  state: "Confirmed",
  occupancy: "Double",
  confirmedAtUtc: "2026-08-03T07:00:00Z",
  journey: {
    packageName: "NoorPath Family Umrah",
    origin: "Delhi",
    departureDate: "2027-10-10",
    returnDate: "2027-10-22",
    makkahHotelName: "Makkah Noor Hotel",
    makkahNights: 7,
    madinahHotelName: "Madinah Noor Hotel",
    madinahNights: 5,
    travelRouteSummary: "Delhi to Jeddah · Madinah to Delhi",
  },
  travellers: [{ fullName: "Amina Khan" }, { fullName: "Omar Khan" }],
  commercial: { currency: "INR", total: 220000, paid: 55000, remaining: 165000 },
  payment: { state: "Succeeded", instalments: [] },
  readiness: { documents: "Ready", visa: "InProgress" },
  support: {
    bookingReference: "NP-20260803-CANCEL",
    correlationId: "safe-cancellation-correlation",
  },
};

const estimate = {
  bookingId,
  bookingState: "Confirmed",
  policy: {
    available: true,
    canRequest: true,
    code: "eligible_for_review",
    message:
      "This estimate is eligible for operator review. The immutable policy snapshot will be retained with the request.",
    version: "vs16-approved-v1",
    timeZoneId: "Asia/Kolkata",
    daysBeforeDeparture: 433,
    refundProcessingBusinessDays: 10,
    currency: "INR",
    settledAmount: 55000,
    percentageFee: 5500,
    nonRefundableAmount: 500,
    refundableAmount: 49000,
    requiresOperatorApproval: true,
    feeComponents: [
      {
        code: "policy_percentage_fee",
        label: "Policy fee (10%)",
        amount: 5500,
      },
      {
        code: "non_refundable_component",
        label: "Configured non-refundable component",
        amount: 500,
      },
    ],
  },
  request: null,
  reasonCategories: [
    "PlansChanged",
    "Health",
    "TravelDocuments",
    "Financial",
    "Other",
  ],
};

const requested = {
  ...estimate,
  policy: { ...estimate.policy, canRequest: false },
  request: {
    id: cancellationId,
    state: "Requested",
    customerStatus: "UnderReview",
    reasonCategory: "PlansChanged",
    policyVersion: "vs16-approved-v1",
    version: 1,
    currency: "INR",
    settledAmount: 55000,
    percentageFee: 5500,
    nonRefundableAmount: 500,
    refundableAmount: 49000,
    refundProcessingBusinessDays: 10,
    decisionReason: null,
    failureCode: null,
    requestedAtUtc: "2026-08-03T18:30:00Z",
    updatedAtUtc: "2026-08-03T18:30:00Z",
    decidedAtUtc: null,
    appliedAtUtc: null,
    refundId: null,
    refundState: null,
    refundFailureCode: null,
    refundedAtUtc: null,
  },
};

const queueItem = {
  cancellationId,
  bookingId,
  state: "Requested",
  customerStatus: "UnderReview",
  currency: "INR",
  settledAmount: 55000,
  percentageFee: 5500,
  nonRefundableAmount: 500,
  refundableAmount: 49000,
  policyVersion: "vs16-approved-v1",
  version: 1,
  requestedAtUtc: "2026-08-03T18:30:00Z",
  updatedAtUtc: "2026-08-03T18:30:00Z",
  failureCode: null,
  refund: null,
};

const caseDetail = {
  booking: {
    id: bookingId,
    reference: "NP-20260803-CANCEL",
    state: "Confirmed",
    currency: "INR",
    total: 220000,
    cancelledAtUtc: null,
  },
  cancellation: requested.request,
  calculation: {
    policyVersion: "vs16-approved-v1",
    policyTimeZoneId: "Asia/Kolkata",
    departureAtUtc: "2027-10-10T04:30:00Z",
    daysBeforeDeparture: 433,
    windowMinimumDaysBeforeDeparture: 30,
    feeBasisPoints: 1000,
    currency: "INR",
    settledAmount: 55000,
    percentageFee: 5500,
    nonRefundableAmount: 500,
    refundableAmount: 49000,
    refundProcessingBusinessDays: 10,
  },
  refund: null,
  allowedActions: [
    { code: "approve", label: "Approve cancellation" },
    { code: "reject", label: "Reject cancellation" },
  ],
  audit: [
    {
      action: "cancellation_requested",
      reason: "PlansChanged",
      actorAccountId: "opaque-customer",
      occurredAtUtc: "2026-08-03T18:30:00Z",
    },
  ],
};

test("customer reviews a server-calculated entitlement and submits mandatory review", async ({
  page,
}) => {
  await page.route(`**/api/v1/journeys/${bookingId}`, (route) =>
    route.fulfill({ json: journey }),
  );
  await page.route(`**/api/v1/bookings/${bookingId}/cancellation`, (route) =>
    route.fulfill({ json: estimate }),
  );
  await page.route(
    `**/api/v1/bookings/${bookingId}/cancellation-requests`,
    (route) => route.fulfill({ status: 201, json: requested }),
  );

  await page.goto(`/bookings/${bookingId}/journey`);
  await expect(
    page.getByRole("heading", { name: "Review before requesting" }),
  ).toBeVisible();
  await expect(page.getByText("₹49,000")).toBeVisible();
  await expect(
    page.getByText(/Every request requires operator review/),
  ).toBeVisible();

  await page.getByLabel("Main reason for cancellation").selectOption("Health");
  await page
    .getByRole("button", { name: "Request cancellation review" })
    .click();
  await expect(
    page.getByRole("heading", { name: "Cancellation request under review" }),
  ).toBeVisible();
  await expect(
    page.getByText("Your cancellation request was submitted for operator review."),
  ).toBeVisible();

  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
  await expectNoA11yViolations(page);
});

test("operator receives recoverable stale-version feedback without editing the amount", async ({
  page,
}) => {
  let detailReads = 0;
  await page.route("**/api/v1/operator/cancellations?*", (route) =>
    route.fulfill({ json: { items: [queueItem] } }),
  );
  await page.route(
    `**/api/v1/operator/cancellations/${cancellationId}`,
    (route) => {
      detailReads += 1;
      return route.fulfill({
        json: {
          ...caseDetail,
          cancellation: {
            ...caseDetail.cancellation,
            version: detailReads > 1 ? 2 : 1,
          },
        },
      });
    },
  );
  await page.route(
    `**/api/v1/operator/cancellations/${cancellationId}/approve`,
    (route) =>
      route.fulfill({
        status: 409,
        json: {
          code: "stale_cancellation_version",
          title: "The cancellation case changed. Refresh it before trying again.",
        },
      }),
  );

  await page.goto("/operator/cancellations");
  await page.getByRole("button", { name: "Review case" }).click();
  await expect(
    page.getByRole("heading", { name: "UnderReview" }),
  ).toBeVisible();
  await expect(
    page.getByText(/Amounts are read-only and derived/),
  ).toBeVisible();
  await page
    .getByLabel("Decision or recovery reason")
    .fill("Approved after reviewing the immutable calculation.");
  await page.getByRole("button", { name: "Approve cancellation" }).click();
  await expect(
    page.getByRole("alert").filter({ hasText: "case changed" }),
  ).toBeVisible();
  expect(detailReads).toBeGreaterThan(1);

  await page.setViewportSize({ width: 390, height: 844 });
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
  await expectNoA11yViolations(page);
});

test("authorized refund remains safe when provider execution is disabled", async ({
  page,
}) => {
  const authorized = {
    ...caseDetail,
    booking: { ...caseDetail.booking, state: "Cancelled" },
    cancellation: {
      ...caseDetail.cancellation,
      state: "Applied",
      customerStatus: "RefundPending",
      version: 4,
      refundId,
      refundState: "Authorized",
    },
    refund: {
      refundId,
      currency: "INR",
      entitledAmount: 49000,
      refundedAmount: 0,
      state: "Authorized",
      failureCode: null,
      version: 1,
      updatedAtUtc: "2026-08-03T18:35:00Z",
    },
    allowedActions: [
      {
        code: "execute_refund",
        label: "Execute authorized refund",
        refundId,
      },
    ],
  };
  await page.route("**/api/v1/operator/cancellations?*", (route) =>
    route.fulfill({ json: { items: [{ ...queueItem, state: "Applied" }] } }),
  );
  await page.route(
    `**/api/v1/operator/cancellations/${cancellationId}`,
    (route) => route.fulfill({ json: authorized }),
  );
  await page.route(`**/api/v1/operator/refunds/${refundId}/execute`, (route) =>
    route.fulfill({ status: 503, json: authorized.refund }),
  );

  await page.goto("/operator/cancellations");
  await page.getByRole("button", { name: "Review case" }).click();
  await page
    .getByLabel("Decision or recovery reason")
    .fill("Attempt the already-authorized provider refund.");
  await page
    .getByRole("button", { name: "Execute authorized refund" })
    .click();
  await expect(
    page.getByRole("alert").filter({ hasText: "production execution is not enabled" }),
  ).toBeVisible();
  await expect(page.getByText(/₹0 of ₹49,000 recorded/)).toBeVisible();
});
