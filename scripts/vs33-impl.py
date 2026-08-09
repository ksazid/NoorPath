from pathlib import Path


def replace(path: str, old: str, new: str) -> None:
    file = Path(path)
    text = file.read_text()
    if old not in text:
        raise SystemExit(f"missing replacement anchor in {path}: {old[:100]!r}")
    file.write_text(text.replace(old, new, 1))


records = "src/Modules/NoorPath.Booking.Infrastructure/DepartureHandoverRecords.cs"
replace(
    records,
    "            entity.Property(x => x.CompletedByAccountId).HasMaxLength(120);\n            entity.Property(x => x.FinalNote).HasMaxLength(500);",
    "            entity.Property(x => x.CompletedByAccountId).HasMaxLength(120);\n            entity.Property(x => x.GroupLeaderName).HasMaxLength(120);\n            entity.Property(x => x.FinalNote).HasMaxLength(500);",
)
replace(
    records,
    "    public bool IsCompleted { get; set; }\n    public string? FinalNote { get; set; }",
    "    public bool IsCompleted { get; set; }\n    public string? GroupLeaderName { get; set; }\n    public string? FinalNote { get; set; }",
)

manifest = "apps/api/DepartureManifestEndpoints.cs"
replace(
    manifest,
    '        app.MapPost("/api/v1/operator/departures/{departureId:guid}/manifest/travellers/{travellerId:guid}/operations", UpdateOperationAsync)\n            .RequireAuthorization();',
    '        app.MapPost("/api/v1/operator/departures/{departureId:guid}/manifest/travellers/{travellerId:guid}/operations", UpdateOperationAsync)\n            .RequireAuthorization();\n        app.MapPost("/api/v1/operator/departures/{departureId:guid}/manifest/group-leader", UpdateGroupLeaderAsync)\n            .RequireAuthorization();',
)
replace(
    manifest,
    "        var items = travellerRows.Select(traveller =>\n        {",
    "        var fulfilmentRecord = await bookings.Set<DepartureHandoverRecord>().AsNoTracking()\n            .SingleOrDefaultAsync(item =>\n                item.DepartureId == departureId && item.OperatorId == access.OperatorId,\n                cancellationToken);\n\n        var items = travellerRows.Select(traveller =>\n        {",
)
replace(
    manifest,
    '            summary = new\n            {\n                travellers = items.Length,\n                ready = items.Count(item => item.readiness == "ready"),\n                blocked = items.Count(item => item.readiness == "blocked"),\n                paymentBlocked = items.Count(item => item.blockers.Contains("payment")),\n                documentBlocked = items.Count(item => item.blockers.Contains("documents")),\n                visaBlocked = items.Count(item => item.blockers.Contains("visa")),\n                accommodationBlocked = items.Count(item => item.blockers.Contains("accommodation"))\n            },\n            items',
    '            summary = new\n            {\n                travellers = items.Length,\n                ready = items.Count(item => item.readiness == "ready"),\n                blocked = items.Count(item => item.readiness == "blocked"),\n                paymentBlocked = items.Count(item => item.blockers.Contains("payment")),\n                documentBlocked = items.Count(item => item.blockers.Contains("documents")),\n                visaBlocked = items.Count(item => item.blockers.Contains("visa")),\n                accommodationBlocked = items.Count(item => item.blockers.Contains("accommodation"))\n            },\n            fulfilment = new\n            {\n                groupLeaderName = fulfilmentRecord?.GroupLeaderName,\n                version = fulfilmentRecord?.Version ?? 0,\n                isCompleted = fulfilmentRecord?.IsCompleted ?? false\n            },\n            items',
)
update_group_leader = '''    private static async Task<IResult> UpdateGroupLeaderAsync(
        Guid departureId,
        UpdateDepartureGroupLeaderRequest request,
        HttpContext http,
        IOperatorAccess operators,
        BookingDbContext bookings,
        CatalogueDbContext catalogue,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(http, operators, cancellationToken);
        if (access.Result is not null)
            return access.Result;

        var departureExists = await catalogue.DepartureBatches.AsNoTracking().AnyAsync(
            item => item.Id == departureId && item.OperatorId == access.OperatorId,
            cancellationToken);
        if (!departureExists)
            return Results.NotFound();

        var name = request.Name?.Trim();
        if (name?.Length > 120)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["Group leader name must be 120 characters or fewer."]
            });
        }
        if (string.IsNullOrWhiteSpace(name))
            name = null;

        var record = await bookings.Set<DepartureHandoverRecord>()
            .SingleOrDefaultAsync(item =>
                item.DepartureId == departureId && item.OperatorId == access.OperatorId,
                cancellationToken);

        if (record?.IsCompleted == true)
            return Results.Conflict(new { code = "handover_completed" });

        var currentVersion = record?.Version ?? 0;
        if (currentVersion != request.ExpectedVersion)
            return Results.Conflict(new { code = "departure_operations_stale", currentVersion });

        if (record is null && name is null)
            return Results.Ok(new { groupLeaderName = (string?)null, version = 0, idempotent = true });

        var principal = http.User.GetCurrentPrincipal()!;
        var actor = principal.AccountId.Value;
        var now = timeProvider.GetUtcNow();
        var previousName = record?.GroupLeaderName;

        if (record is null)
        {
            record = new DepartureHandoverRecord
            {
                Id = Guid.NewGuid(),
                DepartureId = departureId,
                OperatorId = access.OperatorId,
                IsCompleted = false,
                GroupLeaderName = name,
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            bookings.Add(record);
        }
        else
        {
            record.GroupLeaderName = name;
            record.Version += 1;
            record.UpdatedAtUtc = now;
        }

        var travellerCount = await (
            from booking in bookings.Bookings.AsNoTracking()
            join traveller in bookings.Travellers.AsNoTracking() on booking.Id equals traveller.BookingId
            where booking.OperatorId == access.OperatorId
                && booking.DepartureId == departureId
                && booking.State == BookingState.Confirmed
            select traveller.Id).CountAsync(cancellationToken);

        bookings.Add(new DepartureHandoverAuditRecord
        {
            Id = Guid.NewGuid(),
            DepartureId = departureId,
            OperatorId = access.OperatorId,
            ActorAccountId = actor,
            Action = name is null ? "group_leader_cleared" : "group_leader_updated",
            Note = name is null
                ? "Accompanying group leader cleared."
                : $"Accompanying group leader set to {name}.",
            PreviousVersion = currentVersion,
            ResultingVersion = record.Version,
            TravellerCount = travellerCount,
            BlockedCount = 0,
            CorrelationId = http.TraceIdentifier,
            OccurredAtUtc = now
        });

        try
        {
            await bookings.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { code = "departure_operations_stale" });
        }

        return Results.Ok(new
        {
            groupLeaderName = record.GroupLeaderName,
            record.Version,
            previousGroupLeaderName = previousName,
            idempotent = false
        });
    }

'''
replace(
    manifest,
    "    private static async Task<(IResult? Result, string OperatorId)> ResolveAccessAsync(",
    update_group_leader + "    private static async Task<(IResult? Result, string OperatorId)> ResolveAccessAsync(",
)
replace(
    manifest,
    "public sealed record UpdateDepartureManifestOperationRequest(\n    string? Note,\n    bool IsAcknowledged,\n    int ExpectedVersion);",
    "public sealed record UpdateDepartureManifestOperationRequest(\n    string? Note,\n    bool IsAcknowledged,\n    int ExpectedVersion);\n\npublic sealed record UpdateDepartureGroupLeaderRequest(string? Name, int ExpectedVersion);",
)

handover = "apps/api/DepartureHandoverEndpoints.cs"
replace(
    handover,
    "            ? new HandoverStateResponse(false, null, null, null, 0)\n            : new HandoverStateResponse(\n                handover.IsCompleted,\n                handover.FinalNote,",
    "            ? new HandoverStateResponse(false, null, null, null, null, 0)\n            : new HandoverStateResponse(\n                handover.IsCompleted,\n                handover.GroupLeaderName,\n                handover.FinalNote,",
)
replace(
    handover,
    "    private sealed record HandoverStateResponse(\n        bool IsCompleted,\n        string? FinalNote,",
    "    private sealed record HandoverStateResponse(\n        bool IsCompleted,\n        string? GroupLeaderName,\n        string? FinalNote,",
)

ui = "apps/web/app/operator/OperatorDepartureManifest.tsx"
replace(
    ui,
    "  summary: {\n    travellers: number;\n    ready: number;\n    blocked: number;\n    paymentBlocked: number;\n    documentBlocked: number;\n    visaBlocked: number;\n    accommodationBlocked: number;\n  };\n  items: ManifestItem[];",
    "  summary: {\n    travellers: number;\n    ready: number;\n    blocked: number;\n    paymentBlocked: number;\n    documentBlocked: number;\n    visaBlocked: number;\n    accommodationBlocked: number;\n  };\n  fulfilment: {\n    groupLeaderName: string | null;\n    version: number;\n    isCompleted: boolean;\n  };\n  items: ManifestItem[];",
)
replace(
    ui,
    '  const [busy, setBusy] = useState("");\n  const [message, setMessage] = useState("");',
    '  const [busy, setBusy] = useState("");\n  const [leaderBusy, setLeaderBusy] = useState(false);\n  const [groupLeaderName, setGroupLeaderName] = useState("");\n  const [message, setMessage] = useState("");',
)
replace(
    ui,
    "      setDrafts(\n        Object.fromEntries(",
    '      setGroupLeaderName(manifest.fulfilment.groupLeaderName ?? "");\n      setDrafts(\n        Object.fromEntries(',
)
save_leader = '''  const saveGroupLeader = async (name: string | null) => {
    if (leaderBusy || state.kind !== "ready") return;
    setLeaderBusy(true);
    setError("");
    setMessage("");
    try {
      const response = await fetch(
        `/api/v1/operator/departures/${departureId}/manifest/group-leader`,
        {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            name,
            expectedVersion: state.manifest.fulfilment.version,
          }),
        },
      );
      if (response.status === 409) {
        const detail = (await response.json()) as { code?: string };
        setError(
          detail.code === "handover_completed"
            ? "The final handover is completed, so the accompanying group leader can no longer be changed."
            : "Departure fulfilment changed in another session. Refresh before retrying.",
        );
        return;
      }
      if (!response.ok) {
        setError("The accompanying group leader could not be saved. Review the name and retry.");
        return;
      }
      setMessage(
        name?.trim()
          ? "Accompanying group leader saved."
          : "Accompanying group leader cleared.",
      );
      await load();
    } catch {
      setError("Departure fulfilment is temporarily unavailable. Retry when connected.");
    } finally {
      setLeaderBusy(false);
    }
  };

'''
replace(ui, "  return (\n    <OperatorWorkspaceShell", save_leader + "  return (\n    <OperatorWorkspaceShell")
fulfilment_panel = '''            <section className={styles.fulfilmentPanel} aria-label="Departure fulfilment">
              <div className={styles.fulfilmentHeader}>
                <div>
                  <span className={styles.eyebrow}>Departure fulfilment</span>
                  <h2>{state.manifest.departure.packageName}</h2>
                  <p>Keep the package being delivered and the accompanying group leader visible during operations.</p>
                </div>
                <Link
                  className={styles.secondaryButton}
                  href={`/operator/departures/${departureId}/preview`}
                >
                  View package being fulfilled
                </Link>
              </div>
              <div className={styles.leaderRow}>
                <label className={styles.field}>
                  Accompanying group leader
                  <input
                    maxLength={120}
                    value={groupLeaderName}
                    disabled={state.manifest.fulfilment.isCompleted || leaderBusy}
                    onChange={(event) => setGroupLeaderName(event.target.value)}
                    placeholder="Add group leader name"
                  />
                  <span className={styles.fieldHint}>Operational contact only. This does not add a booked traveller.</span>
                </label>
                <div className={styles.leaderActions}>
                  <button
                    className={styles.button}
                    type="button"
                    disabled={state.manifest.fulfilment.isCompleted || leaderBusy || !groupLeaderName.trim()}
                    onClick={() => saveGroupLeader(groupLeaderName)}
                  >
                    {leaderBusy ? "Saving…" : state.manifest.fulfilment.groupLeaderName ? "Update group leader" : "Save group leader"}
                  </button>
                  {state.manifest.fulfilment.groupLeaderName ? (
                    <button
                      className={styles.secondaryButton}
                      type="button"
                      disabled={state.manifest.fulfilment.isCompleted || leaderBusy}
                      onClick={() => saveGroupLeader(null)}
                    >
                      Clear
                    </button>
                  ) : null}
                </div>
              </div>
              {state.manifest.fulfilment.isCompleted ? (
                <p className={styles.fieldHint}>Final handover is complete. Departure fulfilment metadata is read-only.</p>
              ) : null}
            </section>

'''
replace(
    ui,
    '            <section\n              className={styles.summaryGrid}\n              aria-label="Manifest readiness summary"',
    fulfilment_panel + '            <section\n              className={styles.summaryGrid}\n              aria-label="Manifest readiness summary"',
)

css = Path("apps/web/app/operator/OperatorDepartureManifest.module.css")
css.write_text(
    css.read_text()
    + '''\n\n.fulfilmentPanel {\n  display: grid;\n  gap: 1rem;\n  padding: 1rem;\n  border: 1px solid var(--color-border, #d9dedb);\n  border-radius: 1rem;\n  background: var(--color-surface, #fff);\n}\n\n.fulfilmentHeader,\n.leaderRow,\n.leaderActions {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.75rem;\n  align-items: end;\n  justify-content: space-between;\n}\n\n.fulfilmentHeader h2 {\n  margin: 0.2rem 0 0;\n  font-size: 1.1rem;\n}\n\n.fulfilmentHeader p {\n  margin: 0.35rem 0 0;\n  color: var(--color-text-muted, #59645f);\n}\n\n.eyebrow {\n  color: var(--color-text-muted, #59645f);\n  font-size: 0.75rem;\n  font-weight: 800;\n  letter-spacing: 0.06em;\n  text-transform: uppercase;\n}\n\n.leaderRow .field {\n  flex: 1 1 20rem;\n}\n\n.leaderActions {\n  flex: 0 1 auto;\n  justify-content: flex-end;\n}\n\n.fieldHint {\n  color: var(--color-text-muted, #59645f);\n  font-size: 0.8rem;\n  font-weight: 400;\n  line-height: 1.45;\n}\n\n@media (max-width: 520px) {\n  .fulfilmentHeader > *,\n  .leaderRow > *,\n  .leaderActions,\n  .leaderActions > * {\n    width: 100%;\n  }\n}\n'''
)

handover_ui = "apps/web/app/operator/OperatorDepartureHandover.tsx"
replace(
    handover_ui,
    "  handover: {\n    isCompleted: boolean;\n    finalNote: string | null;",
    "  handover: {\n    isCompleted: boolean;\n    groupLeaderName: string | null;\n    finalNote: string | null;",
)
replace(
    handover_ui,
    '              <p>\n                {state.value.departure.origin} ·{" "}\n                {state.value.departure.departureDate} to{" "}\n                {state.value.departure.returnDate}\n              </p>\n            </section>',
    '              <p>\n                {state.value.departure.origin} ·{" "}\n                {state.value.departure.departureDate} to{" "}\n                {state.value.departure.returnDate}\n              </p>\n              <p>\n                Accompanying group leader: {state.value.handover.groupLeaderName ?? "Not assigned"}\n              </p>\n              <Link\n                className={styles.manifestLink}\n                href={`/operator/departures/${departureId}/preview`}\n              >\n                View package being fulfilled\n              </Link>\n            </section>',
)

test_file = Path("tests/NoorPath.Commercial.Integration.Tests/OperatorDepartureHandoverApiTests.cs")
test_text = test_file.read_text()
anchor = "    private static async Task EnsureReadinessSchemasAsync("
if anchor not in test_text:
    raise SystemExit("missing integration-test anchor")
additions = '''    [Fact]
    public async Task Group_leader_assignment_is_versioned_audited_and_does_not_change_travellers()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        await EnsureReadinessSchemasAsync(app, TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        Guid departureId;
        int travellerCountBefore;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var booking = await bookings.Bookings.AsNoTracking()
                .SingleAsync(item => item.Id == OperatorBookingAmendmentApi.OwnedBookingId, TestContext.Current.CancellationToken);
            departureId = booking.DepartureId;
            travellerCountBefore = await bookings.Travellers.CountAsync(TestContext.Current.CancellationToken);
        }

        var response = await client.PostAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/manifest/group-leader",
            new { name = "Amina Rahman", expectedVersion = 0 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var verifyScope = app.Services.CreateAsyncScope();
        var database = verifyScope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var handover = await database.Set<DepartureHandoverRecord>().AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Amina Rahman", handover.GroupLeaderName);
        Assert.Equal(1, handover.Version);
        Assert.False(handover.IsCompleted);
        Assert.Equal(travellerCountBefore, await database.Travellers.CountAsync(TestContext.Current.CancellationToken));
        var audit = await database.Set<DepartureHandoverAuditRecord>().AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("group_leader_updated", audit.Action);
        Assert.Equal(0, audit.PreviousVersion);
        Assert.Equal(1, audit.ResultingVersion);
    }

    [Fact]
    public async Task Group_leader_write_is_safe_not_found_for_foreign_departure()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        await EnsureReadinessSchemasAsync(app, TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/operator/departures/{Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")}/manifest/group-leader",
            new { name = "Foreign Leader", expectedVersion = 0 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var verifyScope = app.Services.CreateAsyncScope();
        var database = verifyScope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Assert.Empty(await database.Set<DepartureHandoverRecord>().ToArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Completed_handover_rejects_group_leader_change()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        await EnsureReadinessSchemasAsync(app, TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        Guid departureId;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var booking = await bookings.Bookings.AsNoTracking()
                .SingleAsync(item => item.Id == OperatorBookingAmendmentApi.OwnedBookingId, TestContext.Current.CancellationToken);
            departureId = booking.DepartureId;
            var now = DateTimeOffset.UtcNow;
            bookings.Add(new DepartureHandoverRecord
            {
                Id = Guid.NewGuid(),
                DepartureId = departureId,
                OperatorId = booking.OperatorId,
                IsCompleted = true,
                GroupLeaderName = "Original Leader",
                FinalNote = "Closeout complete.",
                CompletedByAccountId = OperatorBookingAmendmentApi.OperatorAccount,
                CompletedAtUtc = now,
                Version = 2,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            await bookings.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await client.PostAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/manifest/group-leader",
            new { name = "Changed Leader", expectedVersion = 2 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("handover_completed", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);

        await using var verifyScope = app.Services.CreateAsyncScope();
        var database = verifyScope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var record = await database.Set<DepartureHandoverRecord>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Original Leader", record.GroupLeaderName);
        Assert.Equal(2, record.Version);
    }

'''
test_file.write_text(test_text.replace(anchor, additions + anchor, 1))

Path("apps/web/e2e/departure-fulfilment-group-leader.spec.ts").write_text('''import { expect, test, type Page } from "@playwright/test";

const departureId = "11111111-1111-1111-1111-111111111111";

async function mockManifest(page: Page) {
  let leader: string | null = null;
  let version = 0;

  await page.route("**/manifest/group-leader", async (route) => {
    const body = route.request().postDataJSON() as {
      name?: string | null;
      expectedVersion: number;
    };
    if (body.expectedVersion !== version) {
      await route.fulfill({
        status: 409,
        json: { code: "departure_operations_stale", currentVersion: version },
      });
      return;
    }
    leader = body.name?.trim() || null;
    version += 1;
    await route.fulfill({
      status: 200,
      json: { groupLeaderName: leader, version },
    });
  });

  await page.route("**/manifest", (route) =>
    route.fulfill({
      status: 200,
      json: {
        departure: {
          id: departureId,
          packageName: "NoorPath Essential Umrah",
          origin: "Mumbai",
          departureDate: "2026-10-10",
          returnDate: "2026-10-20",
        },
        summary: {
          travellers: 2,
          ready: 1,
          blocked: 1,
          paymentBlocked: 0,
          documentBlocked: 0,
          visaBlocked: 1,
          accommodationBlocked: 0,
        },
        fulfilment: { groupLeaderName: leader, version, isCompleted: false },
        items: [],
      },
    }),
  );
}

test("departure fulfilment links the package and saves an accompanying group leader", async ({
  page,
}) => {
  await mockManifest(page);
  await page.goto(`/operator/departures/${departureId}/manifest`);

  const packageLink = page.getByRole("link", {
    name: "View package being fulfilled",
  });
  await expect(packageLink).toHaveAttribute(
    "href",
    `/operator/departures/${departureId}/preview`,
  );

  await page.getByLabel("Accompanying group leader").fill("Amina Rahman");
  await page.getByRole("button", { name: "Save group leader" }).click();
  await expect(page.getByRole("status")).toContainText(
    "Accompanying group leader saved",
  );
  await expect(page.getByLabel("Accompanying group leader")).toHaveValue(
    "Amina Rahman",
  );
  await expect(
    page.getByText(
      "Operational contact only. This does not add a booked traveller.",
    ),
  ).toBeVisible();

  await page.getByRole("button", { name: "Clear" }).click();
  await expect(page.getByRole("status")).toContainText(
    "Accompanying group leader cleared",
  );
  await expect(page.getByLabel("Accompanying group leader")).toHaveValue("");
});

test("departure fulfilment reflows on mobile with usable controls", async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await mockManifest(page);
  await page.goto(`/operator/departures/${departureId}/manifest`);

  await page.getByLabel("Accompanying group leader").fill("Amina Rahman");
  const save = page.getByRole("button", { name: "Save group leader" });
  const box = await save.boundingBox();
  expect(box?.height ?? 0).toBeGreaterThanOrEqual(44);

  const overflow = await page.evaluate(
    () =>
      document.documentElement.scrollWidth >
      document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});
''')
