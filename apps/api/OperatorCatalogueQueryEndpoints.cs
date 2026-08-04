using Microsoft.EntityFrameworkCore;
using NoorPath.Catalogue;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Operators;

public static class OperatorCatalogueQueryEndpoints
{
    public static void MapOperatorCatalogueQueries(this WebApplication app)
    {
        app.MapGet("/api/v1/operator/catalogue", ListAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> ListAsync(
        HttpContext http,
        IOperatorAccess operators,
        CatalogueDbContext catalogue,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return Results.Unauthorized();

        var access = await operators.FindActiveMembershipAsync(
            principal.AccountId,
            cancellationToken);
        if (access is null || !access.IsAllowed(OperatorPermissions.AdminAccess))
            return Results.Forbid();

        var items = await (
            from departure in catalogue.DepartureBatches.AsNoTracking()
            join packageVersion in catalogue.PackageVersions.AsNoTracking()
                on departure.PackageVersionId equals packageVersion.Id
            join template in catalogue.PackageTemplates.AsNoTracking()
                on packageVersion.PackageTemplateId equals template.Id
            where departure.OperatorId == access.OperatorId
            orderby departure.DepartureDate, packageVersion.Name
            select new
            {
                departureId = departure.Id,
                packageTemplateId = template.Id,
                packageVersionId = packageVersion.Id,
                packageName = packageVersion.Name,
                summary = packageVersion.Summary,
                origin = departure.Origin,
                departureDate = departure.DepartureDate,
                returnDate = departure.ReturnDate,
                status = StatusKey(departure.Status),
                version = departure.Version,
                updatedAtUtc = departure.UpdatedAtUtc
            }).ToArrayAsync(cancellationToken);

        return Results.Ok(new { items });
    }

    private static string StatusKey(CatalogueDraftStatus status) => status switch
    {
        CatalogueDraftStatus.ReadyForReview => "readyForReview",
        CatalogueDraftStatus.Published => "published",
        _ => "draft"
    };
}
