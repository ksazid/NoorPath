using NoorPath.Catalogue;

namespace NoorPath.Catalogue.Tests;

public sealed class TravelFactsTests
{
    [Fact]
    public void Pending_leg_can_preserve_partial_supplier_facts()
    {
        var draft = new PackageTravelFactsDraft([
            new FlightLegDraft(
                "Saudia",
                "SV",
                "",
                "Chhatrapati Shivaji Maharaj International Airport",
                "BOM",
                "King Abdulaziz International Airport",
                "JED",
                FactConfirmationState.Pending)
        ]);

        var leg = Assert.Single(draft.Legs);
        Assert.Equal("SV", leg.AirlineCode);
        Assert.Equal("BOM", leg.DepartureAirportCode);
        Assert.Equal(FactConfirmationState.Pending, leg.ConfirmationState);
    }

    [Fact]
    public void Confirmed_leg_requires_airline_flight_and_both_airports()
    {
        var errors = PackageTravelFactsDraft.Validate([
            new FlightLegDraft(
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                FactConfirmationState.Confirmed)
        ]);

        Assert.Contains("legs[0].airlineName", errors.Keys);
        Assert.Contains("legs[0].flightNumber", errors.Keys);
        Assert.Contains("legs[0].departureAirportName", errors.Keys);
        Assert.Contains("legs[0].departureAirportCode", errors.Keys);
        Assert.Contains("legs[0].arrivalAirportName", errors.Keys);
        Assert.Contains("legs[0].arrivalAirportCode", errors.Keys);
    }

    [Fact]
    public void Normalization_trims_names_and_uppercases_codes()
    {
        var draft = new PackageTravelFactsDraft([
            new FlightLegDraft(
                "  Emirates  ",
                " ek ",
                " ek500 ",
                " Dubai International Airport ",
                " dxb ",
                " Chhatrapati Shivaji Maharaj International Airport ",
                " bom ",
                FactConfirmationState.Confirmed)
        ]);

        var leg = Assert.Single(draft.Legs);
        Assert.Equal("Emirates", leg.AirlineName);
        Assert.Equal("EK", leg.AirlineCode);
        Assert.Equal("EK500", leg.FlightNumber);
        Assert.Equal("DXB", leg.DepartureAirportCode);
        Assert.Equal("BOM", leg.ArrivalAirportCode);
    }
}
