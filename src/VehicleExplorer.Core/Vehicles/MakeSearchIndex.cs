using System.Text;

namespace VehicleExplorer.Core.Vehicles;

/// <summary>
/// In-memory search over all NHTSA makes (about 12,000). Registered as a singleton: it is loaded once
/// and searched on every keystroke, so a search is a plain scan with no HTTP call and no deserialization.
/// Ranking: exact match, then starts with, then a word starts with, then contains;
/// within each level popular makes come first, then by name.
/// </summary>
public sealed class MakeSearchIndex
{
    private static readonly HashSet<string> _popularMakeNames =
    [
        "TOYOTA", "HONDA", "FORD", "CHEVROLET", "NISSAN", "BMW", "MERCEDES BENZ", "HYUNDAI", "KIA",
        "VOLKSWAGEN", "AUDI", "LEXUS", "JEEP", "SUBARU", "MAZDA", "TESLA", "DODGE", "GMC", "PORSCHE", "VOLVO",
    ];

    // Replaced as a whole on every load, so a search always sees either the old list or the new one.
    private volatile IndexedMake[] _indexedMakes = [];

    public int Count => _indexedMakes.Length;

    public bool IsLoaded => Count > 0;

    /// <summary>Fetches all makes and replaces the index. The client is passed in, never stored.</summary>
    public async Task LoadAsync(INhtsaClient nhtsaClient, CancellationToken cancellationToken)
    {
        var makes = await nhtsaClient.GetMakesAsync(cancellationToken);
        _indexedMakes =
        [
            .. makes.Select(make =>
            {
                var normalizedName = Normalize(make.Name);
                return new IndexedMake(make, normalizedName, _popularMakeNames.Contains(normalizedName));
            }),
        ];
    }

    /// <summary>Loads the index only if it is empty (the warm-up service normally loads it at startup).</summary>
    public async Task EnsureLoadedAsync(INhtsaClient nhtsaClient, CancellationToken cancellationToken)
    {
        if (!IsLoaded)
        {
            await LoadAsync(nhtsaClient, cancellationToken);
        }
    }

    /// <summary>Returns the make with this id, or null if NHTSA has no such make.</summary>
    /// <remarks>A linear scan of about 12,000 integers takes microseconds, so no extra lookup table is kept.</remarks>
    public VehicleMake? FindMake(int makeId) =>
        _indexedMakes.FirstOrDefault(indexedMake => indexedMake.Make.Id == makeId)?.Make;

    public IReadOnlyList<VehicleMake> Search(string search, int limit)
    {
        var term = Normalize(search);
        if (term.Length == 0)
        {
            return [];
        }

        var wordStartTerm = " " + term;
        return
        [
            .. _indexedMakes
                .Select(indexedMake => (indexedMake, matchRank: GetMatchRank(indexedMake.NormalizedName, term, wordStartTerm)))
                .Where(match => match.matchRank is not null)
                .OrderBy(match => match.matchRank)
                .ThenByDescending(match => match.indexedMake.IsPopular)
                .ThenBy(match => match.indexedMake.NormalizedName, StringComparer.Ordinal)
                .Take(limit)
                .Select(match => match.indexedMake.Make),
        ];
    }

    private static MatchRank? GetMatchRank(string name, string term, string wordStartTerm)
    {
        if (name == term)
        {
            return MatchRank.Exact;
        }

        if (name.StartsWith(term, StringComparison.Ordinal))
        {
            return MatchRank.StartsWith;
        }

        if (name.Contains(wordStartTerm, StringComparison.Ordinal))
        {
            return MatchRank.WordStartsWith;
        }

        return name.Contains(term, StringComparison.Ordinal) ? MatchRank.Contains : null;
    }

    /// <summary>
    /// Upper-cases and turns every run of punctuation or spaces into one space,
    /// so "mercedes benz", "Mercedes-Benz" and "MERCEDES-BENZ" are equal.
    /// </summary>
    private static string Normalize(string value)
    {
        var builder = new StringBuilder(value.Length);
        var isWordBreakPending = false;
        foreach (var character in value)
        {
            if (!char.IsLetterOrDigit(character))
            {
                isWordBreakPending = true;
                continue;
            }

            if (isWordBreakPending && builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(char.ToUpperInvariant(character));
            isWordBreakPending = false;
        }

        return builder.ToString();
    }

    private sealed record IndexedMake(VehicleMake Make, string NormalizedName, bool IsPopular);

    private enum MatchRank
    {
        Exact,
        StartsWith,
        WordStartsWith,
        Contains,
    }
}
