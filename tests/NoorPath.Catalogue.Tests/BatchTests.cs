using NoorPath.Catalogue;
using Xunit;

namespace NoorPath.Catalogue.Tests;

public sealed class PackageDepartureDraftTests
{
    private static PackageDepartureDraftDetails Valid() => new(
        " Noor Harmony 12 Nights ",
        " A guided Umrah journey with clearly labelled travel facts. ",
        new(" Makkah Hotel ", "4 star", "850 m from Masjid al-Haram", 6, FactConfirmationState.Confirmed),
        new(" Madinah Hotel ", "4 star", "450 m from Al-Masjid an-Nabawi", 5, FactConfirmationState.Pending),
        new(" Delhi → Jeddah → Makkah → Madinah ", "Flight details pending final confirmation.", FactConfirmationState.Pending),
        " Delhi (DEL) ",
        new(2026, 10, 10),
        new(2026, 10, 22),
        [" Return flights ", "Breakfast", "return flights", ""],
        ["Personal expenses", " Personal expenses ", ""]);

    [Fact]
    public void Draft_normalizes_text_and_ordered_content()
    {
        var draft = new PackageDepartureDraft(Valid());

        Assert.Equal("Noor Harmony 12 Nights", draft.Details.PackageName);
        Assert.Equal("Delhi (DEL)", draft.Details.Origin);
        Assert.Equal(["Return flights", "Breakfast"], draft.Details.Inclusions);
        Assert.Equal(["Personal expenses"], draft.Details.Exclusions);
    }

    [Fact]
    public void Draft_requires_independent_accommodation_and_valid_dates()
    {
        var value = Valid() with
        {
            Makkah = Valid().Makkah with { HotelName = "", Nights = -1 },
            Madinah = Valid().Madinah with { Nights = 0 },
            ReturnDate = new(2026, 10, 9)
        };

        var error = Assert.Throws<CatalogueDraftValidationException>(() => new PackageDepartureDraft(value));

        Assert.Contains("makkah.hotelName", error.Errors.Keys);
        Assert.Contains("makkah.nights", error.Errors.Keys);
        Assert.Contains("returnDate", error.Errors.Keys);
    }

    [Fact]
    public void Draft_requires_at_least_one_stay_night()
    {
        var value = Valid() with
        {
            Makkah = Valid().Makkah with { Nights = 0 },
            Madinah = Valid().Madinah with { Nights = 0 }
        };

        var error = Assert.Throws<CatalogueDraftValidationException>(() => new PackageDepartureDraft(value));

        Assert.Contains("stays", error.Errors.Keys);
    }
}
