using NoorPath.FamilyBooking;
using Xunit;

namespace NoorPath.FamilyBooking.Tests;

public sealed class FamilyBookingPolicyTests
{
    [Fact]
    public void Membership_rejects_duplicate_traveller()
    {
        var travellerId = Guid.NewGuid();

        var error = FamilyBookingPolicy.ValidateMembership([travellerId], travellerId);

        Assert.Equal("The traveller is already in this family party.", error);
    }

    [Fact]
    public void Membership_rejects_party_above_limit()
    {
        var existing = Enumerable.Range(0, FamilyBookingPolicy.MaximumPartySize)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        var error = FamilyBookingPolicy.ValidateMembership(existing, Guid.NewGuid());

        Assert.Contains(FamilyBookingPolicy.MaximumPartySize.ToString(), error);
    }

    [Fact]
    public void Mahram_link_rejects_self_link()
    {
        var travellerId = Guid.NewGuid();

        var error = FamilyBookingPolicy.ValidateMahramLink(
            travellerId,
            travellerId,
            "I confirm this relationship.",
            [travellerId],
            []);

        Assert.Equal("A traveller cannot be linked to themselves as Mahram.", error);
    }

    [Fact]
    public void Mahram_link_requires_both_travellers_in_same_party()
    {
        var protectedTravellerId = Guid.NewGuid();
        var mahramTravellerId = Guid.NewGuid();

        var error = FamilyBookingPolicy.ValidateMahramLink(
            protectedTravellerId,
            mahramTravellerId,
            "I confirm this relationship.",
            [protectedTravellerId],
            []);

        Assert.Equal("Both travellers must belong to the same family party.", error);
    }

    [Fact]
    public void Mahram_link_rejects_duplicate_active_link()
    {
        var protectedTravellerId = Guid.NewGuid();
        var mahramTravellerId = Guid.NewGuid();

        var error = FamilyBookingPolicy.ValidateMahramLink(
            protectedTravellerId,
            mahramTravellerId,
            "I confirm this relationship.",
            [protectedTravellerId, mahramTravellerId],
            [(protectedTravellerId, mahramTravellerId)]);

        Assert.Equal("This active Mahram link already exists.", error);
    }

    [Fact]
    public void Mahram_link_accepts_valid_declaration()
    {
        var protectedTravellerId = Guid.NewGuid();
        var mahramTravellerId = Guid.NewGuid();

        var error = FamilyBookingPolicy.ValidateMahramLink(
            protectedTravellerId,
            mahramTravellerId,
            "I confirm this relationship.",
            [protectedTravellerId, mahramTravellerId],
            []);

        Assert.Null(error);
    }
}
