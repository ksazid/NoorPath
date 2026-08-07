using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Operators;

public static class AccommodationAssignmentEndpoints
{
    public static void MapAccommodationAssignments(this WebApplication app)
    {
        app.MapGet("/api/v1/operator/bookings/{bookingId:guid}/accommodation", GetAsync)
            .RequireAuthorization();
        app.MapPost("/api/v1/operator/bookings/{bookingId:guid}/accommodation/rooms", CreateRoomAsync)
            .RequireAuthorization();
        app.MapPost("/api/v1/operator/bookings/{bookingId:guid}/accommodation/rooms/{roomId:guid}/assign", AssignAsync)
            .RequireAuthorization();
        app.MapPost("/api/v1/operator/bookings/{bookingId:guid}/accommodation/rooms/{roomId:guid}/unassign", UnassignAsync)
            .RequireAuthorization();
        app.MapPost("/api/v1/operator/bookings/{bookingId:guid}/accommodation/rooms/{roomId:guid}/lock", LockAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetAsync(
        Guid bookingId,
        HttpContext http,
        IOperatorAccess operators,
        BookingDbContext bookings,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(http, operators, cancellationToken);
        if (access.Result is not null)
            return access.Result;

        var booking = await bookings.Bookings.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == bookingId && item.OperatorId == access.OperatorId,
                cancellationToken);
        if (booking is null)
            return Results.NotFound();

        var travellers = await bookings.Travellers.AsNoTracking()
            .Where(item => item.BookingId == booking.Id)
            .OrderBy(item => item.Position)
            .Select(item => new
            {
                item.TravellerId,
                item.Position,
                item.FullName
            })
            .ToArrayAsync(cancellationToken);

        var rooms = await bookings.Set<AccommodationRoomRecord>().AsNoTracking()
            .Where(item => item.BookingId == booking.Id && item.OperatorId == access.OperatorId)
            .OrderBy(item => item.Stay)
            .ThenBy(item => item.Label)
            .ToArrayAsync(cancellationToken);
        var assignments = await bookings.Set<AccommodationAssignmentRecord>().AsNoTracking()
            .Where(item => item.BookingId == booking.Id && item.OperatorId == access.OperatorId)
            .ToArrayAsync(cancellationToken);
        var audits = await bookings.Set<AccommodationAssignmentAuditRecord>().AsNoTracking()
            .Where(item => item.BookingId == booking.Id && item.OperatorId == access.OperatorId)
            .OrderByDescending(item => item.OccurredAtUtc)
            .Take(50)
            .Select(item => new
            {
                auditId = item.Id,
                item.TravellerId,
                item.PreviousRoomId,
                item.RoomId,
                stay = item.Stay.ToString().ToLowerInvariant(),
                item.Action,
                item.Reason,
                item.OccurredAtUtc
            })
            .ToArrayAsync(cancellationToken);

        var roomProjection = rooms.Select(room => new
        {
            roomId = room.Id,
            stay = room.Stay.ToString().ToLowerInvariant(),
            roomType = room.RoomType.ToString().ToLowerInvariant(),
            room.Label,
            capacity = AccommodationAssignmentPolicy.Capacity(room.RoomType),
            room.Version,
            room.IsLocked,
            occupants = assignments
                .Where(item => item.RoomId == room.Id)
                .Select(item => item.TravellerId)
                .ToArray()
        }).ToArray();

        var unassigned = Enum.GetValues<AccommodationStay>()
            .Select(stay => new
            {
                stay = stay.ToString().ToLowerInvariant(),
                travellerIds = travellers
                    .Where(traveller => !assignments.Any(item =>
                        item.Stay == stay && item.TravellerId == traveller.TravellerId))
                    .Select(item => item.TravellerId)
                    .ToArray()
            })
            .ToArray();

        return Results.Ok(new
        {
            bookingId = booking.Id,
            reference = booking.Reference,
            bookingState = booking.State.ToString().ToLowerInvariant(),
            bookingOccupancy = booking.Occupancy.ToString().ToLowerInvariant(),
            travellers,
            rooms = roomProjection,
            unassigned,
            history = audits
        });
    }

    private static async Task<IResult> CreateRoomAsync(
        Guid bookingId,
        CreateAccommodationRoomRequest request,
        HttpContext http,
        IOperatorAccess operators,
        BookingDbContext bookings,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(http, operators, cancellationToken);
        if (access.Result is not null)
            return access.Result;

        var booking = await bookings.Bookings.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == bookingId && item.OperatorId == access.OperatorId,
                cancellationToken);
        if (booking is null)
            return Results.NotFound();
        if (booking.State != BookingState.Confirmed)
            return Results.Conflict(new { code = "booking_not_confirmed" });

        if (!TryStay(request.Stay, out var stay)
            || !TryRoomType(request.RoomType, out var roomType))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["room"] = ["Stay and room type must be valid."]
            });
        }

        var label = request.Label?.Trim() ?? string.Empty;
        if (label.Length is 0 or > 80)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["label"] = ["Room label must be between 1 and 80 characters."]
            });
        }

        var exists = await bookings.Set<AccommodationRoomRecord>().AsNoTracking()
            .AnyAsync(item =>
                item.BookingId == booking.Id
                && item.Stay == stay
                && item.Label == label,
                cancellationToken);
        if (exists)
            return Results.Conflict(new { code = "room_label_exists" });

        var now = timeProvider.GetUtcNow();
        var room = new AccommodationRoomRecord
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            OperatorId = access.OperatorId!,
            Stay = stay,
            RoomType = roomType,
            Label = label,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        bookings.Set<AccommodationRoomRecord>().Add(room);
        await bookings.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/v1/operator/bookings/{booking.Id}/accommodation",
            new
            {
                roomId = room.Id,
                stay = room.Stay.ToString().ToLowerInvariant(),
                roomType = room.RoomType.ToString().ToLowerInvariant(),
                room.Label,
                capacity = AccommodationAssignmentPolicy.Capacity(room.RoomType),
                room.Version
            });
    }

    private static async Task<IResult> AssignAsync(
        Guid bookingId,
        Guid roomId,
        AccommodationAssignmentRequest request,
        HttpContext http,
        IOperatorAccess operators,
        BookingDbContext bookings,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(http, operators, cancellationToken);
        if (access.Result is not null)
            return access.Result;

        await using var transaction = await bookings.Database.BeginTransactionAsync(cancellationToken);
        var booking = await bookings.Bookings
            .SingleOrDefaultAsync(
                item => item.Id == bookingId && item.OperatorId == access.OperatorId,
                cancellationToken);
        var room = await bookings.Set<AccommodationRoomRecord>()
            .SingleOrDefaultAsync(
                item => item.Id == roomId
                    && item.BookingId == bookingId
                    && item.OperatorId == access.OperatorId,
                cancellationToken);
        if (booking is null || room is null)
            return Results.NotFound();

        var travellerIds = await bookings.Travellers.AsNoTracking()
            .Where(item => item.BookingId == booking.Id)
            .Select(item => item.TravellerId)
            .ToArrayAsync(cancellationToken);
        var records = await bookings.Set<AccommodationAssignmentRecord>()
            .Where(item => item.BookingId == booking.Id && item.Stay == room.Stay)
            .ToArrayAsync(cancellationToken);
        var domainAssignments = records
            .Select(item => new AccommodationAssignment(item.TravellerId, item.RoomId, item.Stay))
            .ToArray();

        try
        {
            AccommodationAssignmentPolicy.ValidateMutation(
                booking.State,
                ToDomain(room),
                request.TravellerId,
                travellerIds,
                domainAssignments,
                request.Reason ?? string.Empty,
                request.ExpectedRoomVersion);
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["assignment"] = [exception.Message]
            });
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new { code = "assignment_conflict", message = exception.Message });
        }

        var existing = records.SingleOrDefault(item => item.TravellerId == request.TravellerId);
        AccommodationRoomRecord? previousRoom = null;
        if (existing is not null)
        {
            previousRoom = await bookings.Set<AccommodationRoomRecord>()
                .SingleAsync(item => item.Id == existing.RoomId, cancellationToken);
            if (request.ExpectedPreviousRoomVersion is null
                || request.ExpectedPreviousRoomVersion != previousRoom.Version)
            {
                return Results.Conflict(new
                {
                    code = "previous_room_stale",
                    message = "The traveller's current room changed. Refresh before reassigning."
                });
            }
        }

        var now = timeProvider.GetUtcNow();
        var previousRoomId = existing?.RoomId;
        var previousDestinationVersion = room.Version;
        if (existing is null)
        {
            bookings.Set<AccommodationAssignmentRecord>().Add(new AccommodationAssignmentRecord
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                OperatorId = access.OperatorId!,
                RoomId = room.Id,
                TravellerId = request.TravellerId,
                Stay = room.Stay,
                AssignedAtUtc = now
            });
        }
        else
        {
            existing.RoomId = room.Id;
            existing.AssignedAtUtc = now;
            previousRoom!.Version++;
            previousRoom.UpdatedAtUtc = now;
        }

        room.Version++;
        room.UpdatedAtUtc = now;
        bookings.Set<AccommodationAssignmentAuditRecord>().Add(new AccommodationAssignmentAuditRecord
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            OperatorId = access.OperatorId!,
            ActorAccountId = access.AccountId!,
            TravellerId = request.TravellerId,
            PreviousRoomId = previousRoomId,
            RoomId = room.Id,
            Stay = room.Stay,
            Action = existing is null ? "assigned" : "reassigned",
            Reason = request.Reason!.Trim(),
            PreviousRoomVersion = previousDestinationVersion,
            ResultingRoomVersion = room.Version,
            CorrelationId = http.TraceIdentifier,
            OccurredAtUtc = now
        });

        try
        {
            await bookings.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.Conflict(new { code = "room_stale" });
        }

        return Results.Ok(new
        {
            roomId = room.Id,
            travellerId = request.TravellerId,
            roomVersion = room.Version,
            action = existing is null ? "assigned" : "reassigned"
        });
    }

    private static async Task<IResult> UnassignAsync(
        Guid bookingId,
        Guid roomId,
        AccommodationAssignmentRequest request,
        HttpContext http,
        IOperatorAccess operators,
        BookingDbContext bookings,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(http, operators, cancellationToken);
        if (access.Result is not null)
            return access.Result;

        var booking = await bookings.Bookings
            .SingleOrDefaultAsync(
                item => item.Id == bookingId && item.OperatorId == access.OperatorId,
                cancellationToken);
        var room = await bookings.Set<AccommodationRoomRecord>()
            .SingleOrDefaultAsync(item =>
                item.Id == roomId
                && item.BookingId == bookingId
                && item.OperatorId == access.OperatorId,
                cancellationToken);
        if (booking is null || room is null)
            return Results.NotFound();

        var records = await bookings.Set<AccommodationAssignmentRecord>()
            .Where(item => item.BookingId == booking.Id && item.Stay == room.Stay)
            .ToArrayAsync(cancellationToken);
        try
        {
            AccommodationAssignmentPolicy.ValidateUnassign(
                booking.State,
                ToDomain(room),
                request.TravellerId,
                records.Select(item => new AccommodationAssignment(item.TravellerId, item.RoomId, item.Stay)).ToArray(),
                request.Reason ?? string.Empty,
                request.ExpectedRoomVersion);
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["assignment"] = [exception.Message]
            });
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new { code = "assignment_conflict", message = exception.Message });
        }

        var assignment = records.Single(item =>
            item.RoomId == room.Id && item.TravellerId == request.TravellerId);
        var previousVersion = room.Version;
        var now = timeProvider.GetUtcNow();
        bookings.Set<AccommodationAssignmentRecord>().Remove(assignment);
        room.Version++;
        room.UpdatedAtUtc = now;
        bookings.Set<AccommodationAssignmentAuditRecord>().Add(new AccommodationAssignmentAuditRecord
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            OperatorId = access.OperatorId!,
            ActorAccountId = access.AccountId!,
            TravellerId = request.TravellerId,
            PreviousRoomId = room.Id,
            RoomId = null,
            Stay = room.Stay,
            Action = "unassigned",
            Reason = request.Reason!.Trim(),
            PreviousRoomVersion = previousVersion,
            ResultingRoomVersion = room.Version,
            CorrelationId = http.TraceIdentifier,
            OccurredAtUtc = now
        });

        try
        {
            await bookings.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { code = "room_stale" });
        }

        return Results.Ok(new { roomId = room.Id, travellerId = request.TravellerId, roomVersion = room.Version });
    }

    private static async Task<IResult> LockAsync(
        Guid bookingId,
        Guid roomId,
        AccommodationRoomLockRequest request,
        HttpContext http,
        IOperatorAccess operators,
        BookingDbContext bookings,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(http, operators, cancellationToken);
        if (access.Result is not null)
            return access.Result;

        var booking = await bookings.Bookings.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == bookingId && item.OperatorId == access.OperatorId,
                cancellationToken);
        var room = await bookings.Set<AccommodationRoomRecord>()
            .SingleOrDefaultAsync(item =>
                item.Id == roomId
                && item.BookingId == bookingId
                && item.OperatorId == access.OperatorId,
                cancellationToken);
        if (booking is null || room is null)
            return Results.NotFound();
        if (room.Version != request.ExpectedRoomVersion)
            return Results.Conflict(new { code = "room_stale" });

        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length is 0 or > AccommodationAssignmentPolicy.MaximumReasonLength)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reason"] = ["Lock reason is required and must be 500 characters or fewer."]
            });
        }

        room.IsLocked = request.Locked;
        room.Version++;
        room.UpdatedAtUtc = timeProvider.GetUtcNow();
        try
        {
            await bookings.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { code = "room_stale" });
        }

        return Results.Ok(new { roomId = room.Id, room.IsLocked, room.Version, reason, correlationId = http.TraceIdentifier });
    }

    private static AccommodationRoom ToDomain(AccommodationRoomRecord room) => new(
        room.Id,
        room.Stay,
        room.RoomType,
        room.Label,
        room.Version,
        room.IsLocked);

    private static async Task<(IResult? Result, string? OperatorId, string? AccountId)> ResolveAccessAsync(
        HttpContext http,
        IOperatorAccess operators,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return (Results.Unauthorized(), null, null);

        var access = await operators.FindActiveMembershipAsync(principal.AccountId, cancellationToken);
        if (access is null || !access.IsAllowed(OperatorPermissions.AdminAccess))
            return (Results.Forbid(), null, null);

        return (null, access.OperatorId, principal.AccountId);
    }

    private static bool TryStay(string? value, out AccommodationStay stay) =>
        Enum.TryParse(value, true, out stay);

    private static bool TryRoomType(string? value, out AccommodationRoomType roomType) =>
        Enum.TryParse(value, true, out roomType);
}

public sealed record CreateAccommodationRoomRequest(
    string? Stay,
    string? RoomType,
    string? Label);

public sealed record AccommodationAssignmentRequest(
    Guid TravellerId,
    string? Reason,
    int ExpectedRoomVersion,
    int? ExpectedPreviousRoomVersion);

public sealed record AccommodationRoomLockRequest(
    bool Locked,
    string? Reason,
    int ExpectedRoomVersion);
