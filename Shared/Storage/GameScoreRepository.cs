using BlockDrop.Shared.Contracts;

namespace BlockDrop.Shared.Storage;

public sealed class GameScoreRepository(string dataDirectory)
{
    public ScoreStore Open(GameDefinition game)
    {
        if (string.IsNullOrWhiteSpace(game.Id) || game.Id.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
            throw new ArgumentException("Game ID is not a valid save directory.", nameof(game));
        if (game.LegacyScoreFile is not null && game.LegacyScoreFile != Path.GetFileName(game.LegacyScoreFile))
            throw new ArgumentException("Legacy save must be a file name.", nameof(game));
        var legacyPath = game.LegacyScoreFile is null ? null : Path.Combine(dataDirectory, game.LegacyScoreFile);
        return new ScoreStore(Path.Combine(dataDirectory, "games", game.Id), legacyPath);
    }
}
