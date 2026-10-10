using Robust.Shared.Utility;

namespace Content.Shared._Floof.Sprite;

public static class SpriteSpecifierHelpers
{
    public static string GetFilename(this SpriteSpecifier specifier)
    {
        return specifier switch
        {
            SpriteSpecifier.Rsi rsi => rsi.RsiState,
            SpriteSpecifier.Texture texture => texture.TexturePath.Filename,
            _ => LogErrorAndReturn("<error>", $"Unknown sprite specifier type: {specifier.GetType()}! Update GetFilename to handle this type.")
        };
    }

    // This is stupid. Why cant we just chain expressions.
    private static string LogErrorAndReturn(string toReturn, string toLog)
    {
        Logger.GetSawmill("sprite-specifier").Error(toLog);
        return toReturn;
    }
}
