using BlockDrop.Shared.Contracts;

namespace BlockDrop.ConsoleUI;

public sealed class GameCatalog
{
    public IReadOnlyList<IRetroGame> Games { get; }
    private readonly Dictionary<string, IRetroGame> byId;

    public GameCatalog(IEnumerable<IRetroGame> games)
    {
        var entries = games.ToArray();
        if (entries.Length == 0) throw new ArgumentException("Register at least one game.", nameof(games));
        byId = new(StringComparer.Ordinal);
        foreach (var game in entries)
        {
            var definition = game.Definition;
            if (string.IsNullOrWhiteSpace(definition.Id) || definition.Id.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
                throw new ArgumentException("Game IDs must use lowercase letters, digits or hyphens.", nameof(games));
            if (string.IsNullOrWhiteSpace(definition.Title)) throw new ArgumentException("Every game needs a title.", nameof(games));
            if (!byId.TryAdd(definition.Id, game)) throw new ArgumentException("Game IDs must be unique.", nameof(games));
        }
        Games = Array.AsReadOnly(entries);
    }

    public IRetroGame Find(string id) => byId.TryGetValue(id, out var game)
        ? game : throw new ArgumentException("Game is not registered: " + id, nameof(id));
}
