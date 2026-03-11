using System;
using MLBStatsAPI.IDs;
using Xunit;

namespace DongBot.Tests;

public class NameIdResolverTests
{
    [Fact]
    public void TryResolveFromStaticIds_Player_ExactName_ReturnsMatch()
    {
        bool found = NameIdResolver.TryResolveFromStaticIds(typeof(PlayerIds), "Ronald Acuna Jr", out NameIdMatch match);

        Assert.True(found);
        Assert.Equal(660670, match.Id);
    }

    [Fact]
    public void TryResolveFromStaticIds_Player_PartialName_ReturnsMatch()
    {
        bool found = NameIdResolver.TryResolveFromStaticIds(typeof(PlayerIds), "Ronald Acuna", out NameIdMatch match);

        Assert.True(found);
        Assert.Equal(660670, match.Id);
    }

    [Fact]
    public void TryResolveFromStaticIds_TeamName_ReturnsTeamId()
    {
        bool found = NameIdResolver.TryResolveFromStaticIds(typeof(TeamIds), "Atlanta Braves", out NameIdMatch match);

        Assert.True(found);
        Assert.Equal(TeamIds.AtlantaBraves, match.Id);
    }

    [Fact]
    public void TryResolveFromStaticIds_VenueName_ReturnsVenueId()
    {
        bool found = NameIdResolver.TryResolveFromStaticIds(typeof(VenueIds), "Truist Park", out NameIdMatch match);

        Assert.True(found);
        Assert.Equal(VenueIds.TruistPark, match.Id);
    }

    [Fact]
    public void TryFindBestPersonInSearchJson_PicksBestNameMatch_NotFirst()
    {
        string json = "{\"people\":[{\"id\":1,\"fullName\":\"Ronald Bolanos\"},{\"id\":660670,\"fullName\":\"Ronald Acuna Jr.\"}]}";

        bool found = NameIdResolver.TryFindBestPersonInSearchJson(json, "Ronald Acuna", out NameIdMatch match);

        Assert.True(found);
        Assert.Equal(660670, match.Id);
        Assert.Equal("Ronald Acuna Jr.", match.Name);
    }

    [Fact]
    public void FindBestMatch_GenericCandidates_ReturnsBestMatch()
    {
        var candidates = new (int Id, string Name)[]
        {
            (1, "Ronald Bolanos"),
            (2, "Ronald Acuna Jr."),
            (3, "Max Fried")
        };

        NameIdMatch? match = NameIdResolver.FindBestMatch(candidates, "Ronald Acuna");

        Assert.True(match.HasValue);
        Assert.Equal(2, match.Value.Id);
    }

    [Fact]
    public void Normalize_RemovesPunctuationAndDiacritics()
    {
        string normalized = NameIdResolver.Normalize("José-Ramírez Jr.");

        Assert.Equal("joseramirezjr", normalized);
    }
}
