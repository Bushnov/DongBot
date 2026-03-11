#nullable enable

using System;
using MLBStatsAPI.IDs;
using MLBStatsAPI.Models;

namespace DongBot
{
    public enum StandingsFilterScope
    {
        Auto,
        Division,
        League,
        Sport
    }

    public readonly record struct StandingsFilter(StandingsFilterScope Scope, string Query);

    public interface IStandingsFilterService
    {
        StandingsFilter ParseFilter(string[] commandParts, StandingsFilterScope requestedScope);
        bool RecordMatches(StandingRecord record, StandingsFilter filter);
    }

    public sealed class StandingsFilterService : IStandingsFilterService
    {
        private readonly INameIdResolver _nameIdResolver;

        public StandingsFilterService(INameIdResolver nameIdResolver)
        {
            _nameIdResolver = nameIdResolver;
        }

        public StandingsFilter ParseFilter(string[] commandParts, StandingsFilterScope requestedScope)
        {
            string filterQuery = commandParts.Length > 1 ? string.Join(" ", commandParts[1..]) : string.Empty;
            StandingsFilterScope scope = requestedScope;

            if (scope == StandingsFilterScope.Auto && commandParts.Length > 2)
            {
                string scopeToken = commandParts[1].ToUpperInvariant();
                if (scopeToken == "DIVISION")
                {
                    scope = StandingsFilterScope.Division;
                    filterQuery = string.Join(" ", commandParts[2..]);
                }
                else if (scopeToken == "LEAGUE")
                {
                    scope = StandingsFilterScope.League;
                    filterQuery = string.Join(" ", commandParts[2..]);
                }
                else if (scopeToken == "SPORT")
                {
                    scope = StandingsFilterScope.Sport;
                    filterQuery = string.Join(" ", commandParts[2..]);
                }
            }

            return new StandingsFilter(scope, filterQuery);
        }

        public bool RecordMatches(StandingRecord record, StandingsFilter filter)
        {
            if (string.IsNullOrWhiteSpace(filter.Query))
            {
                return true;
            }

            bool divisionMatch = MatchesEntity(filter.Query, typeof(DivisionIds), record.Division?.Id, record.Division?.Name);
            bool leagueMatch = MatchesEntity(filter.Query, typeof(LeagueIds), record.League?.Id, record.League?.Name);
            bool sportMatch = MatchesEntity(filter.Query, typeof(SportIds), record.Sport?.Id, record.Sport?.Name);

            return filter.Scope switch
            {
                StandingsFilterScope.Division => divisionMatch,
                StandingsFilterScope.League => leagueMatch,
                StandingsFilterScope.Sport => sportMatch,
                _ => divisionMatch || leagueMatch || sportMatch
            };
        }

        private bool MatchesEntity(string query, Type idsType, int? candidateId, string? candidateName)
        {
            if (_nameIdResolver.TryResolveFromStaticIds(idsType, query, out NameIdMatch mapped) &&
                candidateId.HasValue && candidateId.Value == mapped.Id)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(candidateName))
            {
                return false;
            }

            NameIdMatch? best = _nameIdResolver.FindBestMatch(new[] { (candidateId ?? 0, candidateName) }, query);
            return best.HasValue;
        }
    }
}
