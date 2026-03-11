#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DongBot
{
    public readonly record struct NameIdMatch(int Id, string Name, int Score);

    public interface INameIdResolver
    {
        bool TryResolveFromStaticIds(Type idsClassType, string query, out NameIdMatch match);
        bool TryFindBestPersonInSearchJson(string searchJson, string requestedName, out NameIdMatch match);
        NameIdMatch? FindBestMatch(IEnumerable<(int Id, string Name)> candidates, string query);
        string Normalize(string value);
    }

    internal sealed class DefaultNameIdResolver : INameIdResolver
    {
        public bool TryResolveFromStaticIds(Type idsClassType, string query, out NameIdMatch match)
        {
            match = default;

            if (idsClassType == null || string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            string normalizedQuery = Normalize(query);
            if (normalizedQuery.Length == 0)
            {
                return false;
            }

            NameIdMatch? best = null;

            foreach (FieldInfo field in idsClassType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!field.IsLiteral || field.IsInitOnly || field.FieldType != typeof(int))
                {
                    continue;
                }

                int id = (int)field.GetRawConstantValue()!;
                string displayName = ExpandIdentifier(field.Name);
                int score = ScoreNameMatch(normalizedQuery, Normalize(displayName));
                if (score <= 0)
                {
                    continue;
                }

                NameIdMatch candidate = new(id, displayName, score);
                if (best == null || candidate.Score > best.Value.Score)
                {
                    best = candidate;
                }
            }

            if (best.HasValue)
            {
                match = best.Value;
                return true;
            }

            return false;
        }

        public bool TryFindBestPersonInSearchJson(string searchJson, string requestedName, out NameIdMatch match)
        {
            match = default;

            if (string.IsNullOrWhiteSpace(searchJson) || string.IsNullOrWhiteSpace(requestedName))
            {
                return false;
            }

            string normalizedRequested = Normalize(requestedName);

            using JsonDocument doc = JsonDocument.Parse(searchJson);
            if (!doc.RootElement.TryGetProperty("people", out JsonElement people) || people.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            NameIdMatch? best = null;
            NameIdMatch? first = null;

            foreach (JsonElement person in people.EnumerateArray())
            {
                if (!person.TryGetProperty("id", out JsonElement idEl) || idEl.ValueKind != JsonValueKind.Number)
                {
                    continue;
                }

                int id = idEl.GetInt32();
                string name = person.TryGetProperty("fullName", out JsonElement nameEl)
                    ? (nameEl.GetString() ?? "Unknown")
                    : "Unknown";

                NameIdMatch current = new(id, name, ScoreNameMatch(normalizedRequested, Normalize(name)));
                if (!first.HasValue)
                {
                    first = current;
                }

                if (best == null || current.Score > best.Value.Score)
                {
                    best = current;
                }
            }

            if (best.HasValue && best.Value.Score > 0)
            {
                match = best.Value;
                return true;
            }

            if (first.HasValue)
            {
                match = first.Value;
                return true;
            }

            return false;
        }

        public NameIdMatch? FindBestMatch(IEnumerable<(int Id, string Name)> candidates, string query)
        {
            if (candidates == null || string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            string normalizedQuery = Normalize(query);
            if (normalizedQuery.Length == 0)
            {
                return null;
            }

            NameIdMatch? best = null;

            foreach ((int id, string name) in candidates)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                int score = ScoreNameMatch(normalizedQuery, Normalize(name));
                if (score <= 0)
                {
                    continue;
                }

                NameIdMatch current = new(id, name, score);
                if (best == null || current.Score > best.Value.Score)
                {
                    best = current;
                }
            }

            return best;
        }

        public string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string formD = value.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new(formD.Length);

            foreach (char ch in formD)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
            }

            return sb.ToString();
        }

        private static string ExpandIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return string.Empty;
            }

            StringBuilder sb = new(identifier.Length + 8);
            for (int i = 0; i < identifier.Length; i++)
            {
                char current = identifier[i];

                if (i > 0 && char.IsUpper(current) &&
                    (char.IsLower(identifier[i - 1]) || char.IsDigit(identifier[i - 1])))
                {
                    sb.Append(' ');
                }

                sb.Append(current);
            }

            return sb.ToString();
        }

        private static int ScoreNameMatch(string normalizedQuery, string normalizedCandidate)
        {
            if (normalizedQuery.Length == 0 || normalizedCandidate.Length == 0)
            {
                return 0;
            }

            if (normalizedQuery == normalizedCandidate)
            {
                return 100;
            }

            if (normalizedCandidate.StartsWith(normalizedQuery, StringComparison.Ordinal))
            {
                return 90;
            }

            if (normalizedCandidate.Contains(normalizedQuery, StringComparison.Ordinal))
            {
                return 80;
            }

            if (normalizedQuery.Contains(normalizedCandidate, StringComparison.Ordinal))
            {
                return 70;
            }

            return 0;
        }
    }

    /// <summary>
    /// Reusable resolver for converting human-entered names to numeric IDs using SDK static ID maps,
    /// with helpers for selecting best match from API search payloads.
    /// </summary>
    public static class NameIdResolver
    {
        public static INameIdResolver Default { get; } = new DefaultNameIdResolver();

        public static bool TryResolveFromStaticIds(Type idsClassType, string query, out NameIdMatch match)
            => Default.TryResolveFromStaticIds(idsClassType, query, out match);

        public static bool TryFindBestPersonInSearchJson(string searchJson, string requestedName, out NameIdMatch match)
            => Default.TryFindBestPersonInSearchJson(searchJson, requestedName, out match);

        public static NameIdMatch? FindBestMatch(IEnumerable<(int Id, string Name)> candidates, string query)
            => Default.FindBestMatch(candidates, query);

        public static string Normalize(string value)
            => Default.Normalize(value);
    }
}
