#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MLBStatsAPI.IDs;
using MLBStatsAPI.Models;

namespace DongBot
{
    public enum EntityLookupStatus
    {
        Found,
        NotFound,
        DataUnavailable
    }

    public readonly record struct PlayerLookupResult(EntityLookupStatus Status, int? PlayerId, string? ResolvedName);
    public readonly record struct TeamLookupResult(EntityLookupStatus Status, Team? Team);
    public readonly record struct VenueLookupResult(EntityLookupStatus Status, Venue? Venue, IReadOnlyList<Team> HomeTeams);

    public interface IEntityLookupService
    {
        Task<PlayerLookupResult> ResolvePlayerAsync(string playerName);
        Task<TeamLookupResult> ResolveTeamAsync(string teamQuery);
        Task<VenueLookupResult> ResolveVenueAsync(string venueQuery);
    }

    public sealed class EntityLookupService : IEntityLookupService
    {
        private readonly IMLBDataClient _mlbClient;
        private readonly INameIdResolver _nameIdResolver;

        public EntityLookupService(IMLBDataClient mlbClient, INameIdResolver nameIdResolver)
        {
            _mlbClient = mlbClient;
            _nameIdResolver = nameIdResolver;
        }

        public async Task<PlayerLookupResult> ResolvePlayerAsync(string playerName)
        {
            if (_nameIdResolver.TryResolveFromStaticIds(typeof(PlayerIds), playerName, out NameIdMatch mappedPlayer))
            {
                PeopleResponse? mappedInfo = await _mlbClient.GetPersonAsync(mappedPlayer.Id);
                Person? mappedPerson = mappedInfo?.People?.FirstOrDefault();
                if (mappedPerson != null)
                {
                    return new PlayerLookupResult(EntityLookupStatus.Found, mappedPlayer.Id, mappedPerson.FullName ?? mappedPlayer.Name);
                }
            }

            string searchResult = await _mlbClient.SearchPeopleAsync(playerName, SportIds.MLB);
            if (string.IsNullOrWhiteSpace(searchResult))
            {
                return new PlayerLookupResult(EntityLookupStatus.DataUnavailable, null, null);
            }

            if (_nameIdResolver.TryFindBestPersonInSearchJson(searchResult, playerName, out NameIdMatch apiMatch))
            {
                return new PlayerLookupResult(EntityLookupStatus.Found, apiMatch.Id, apiMatch.Name);
            }

            return new PlayerLookupResult(EntityLookupStatus.NotFound, null, null);
        }

        public async Task<TeamLookupResult> ResolveTeamAsync(string teamQuery)
        {
            TeamsResponse? allTeams = await _mlbClient.GetTeamsAsync(sportId: SportIds.MLB);
            if (allTeams?.Teams == null || !allTeams.Teams.Any())
            {
                return new TeamLookupResult(EntityLookupStatus.DataUnavailable, null);
            }

            string upperQuery = teamQuery.ToUpperInvariant();

            Team? team = allTeams.Teams.FirstOrDefault(t =>
                (t.Name?.ToUpperInvariant().Contains(upperQuery) ?? false) ||
                (t.Abbreviation?.ToUpperInvariant() == upperQuery) ||
                (t.TeamName?.ToUpperInvariant().Contains(upperQuery) ?? false) ||
                (t.LocationName?.ToUpperInvariant().Contains(upperQuery) ?? false));

            if (team == null)
            {
                NameIdMatch? dynamicTeamMatch = _nameIdResolver.FindBestMatch(
                    allTeams.Teams
                        .SelectMany(t => new[]
                        {
                            (t.Id, t.Name ?? string.Empty),
                            (t.Id, t.TeamName ?? string.Empty),
                            (t.Id, t.LocationName ?? string.Empty),
                            (t.Id, t.Abbreviation ?? string.Empty)
                        }),
                    teamQuery);

                if (dynamicTeamMatch.HasValue)
                {
                    team = allTeams.Teams.FirstOrDefault(t => t.Id == dynamicTeamMatch.Value.Id);
                }
            }

            if (team == null && _nameIdResolver.TryResolveFromStaticIds(typeof(TeamIds), teamQuery, out NameIdMatch teamIdMatch))
            {
                team = allTeams.Teams.FirstOrDefault(t => t.Id == teamIdMatch.Id);
            }

            if (team == null)
            {
                return new TeamLookupResult(EntityLookupStatus.NotFound, null);
            }

            return new TeamLookupResult(EntityLookupStatus.Found, team);
        }

        public async Task<VenueLookupResult> ResolveVenueAsync(string venueQuery)
        {
            TeamsResponse? allTeams = await _mlbClient.GetTeamsAsync(sportId: SportIds.MLB);
            if (allTeams?.Teams == null || !allTeams.Teams.Any())
            {
                return new VenueLookupResult(EntityLookupStatus.DataUnavailable, null, Array.Empty<Team>());
            }

            List<Team> teamsWithVenue = allTeams.Teams
                .Where(t => t.Venue != null && t.Venue.Id > 0 && !string.IsNullOrWhiteSpace(t.Venue.Name))
                .ToList();

            if (!teamsWithVenue.Any())
            {
                return new VenueLookupResult(EntityLookupStatus.DataUnavailable, null, Array.Empty<Team>());
            }

            Venue? venue = null;
            List<Team> homeTeams = new();

            if (_nameIdResolver.TryResolveFromStaticIds(typeof(VenueIds), venueQuery, out NameIdMatch mappedVenue))
            {
                homeTeams = teamsWithVenue.Where(t => t.Venue!.Id == mappedVenue.Id).ToList();
                venue = homeTeams.FirstOrDefault()?.Venue;
            }

            if (venue == null)
            {
                NameIdMatch? venueMatch = _nameIdResolver.FindBestMatch(
                    teamsWithVenue.Select(t => (t.Venue!.Id, t.Venue!.Name)),
                    venueQuery);

                if (venueMatch.HasValue)
                {
                    homeTeams = teamsWithVenue.Where(t => t.Venue!.Id == venueMatch.Value.Id).ToList();
                    venue = homeTeams.FirstOrDefault()?.Venue;
                }
            }

            if (venue == null)
            {
                return new VenueLookupResult(EntityLookupStatus.NotFound, null, Array.Empty<Team>());
            }

            return new VenueLookupResult(EntityLookupStatus.Found, venue, homeTeams);
        }
    }
}
