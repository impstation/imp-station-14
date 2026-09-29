using Content.Client.Stylesheets.Palette;

namespace Content.Client._Impstation.Stylesheets;

/// <summary>
/// Colors for the heretic stylesheet.
/// </summary>
public sealed partial class HereticStylesheet
{
    public override ColorPalette PrimaryPalette => ColorPalette.FromHexBase("#c3228e");
    public override ColorPalette SecondaryPalette => ColorPalette.FromHexBase("#40004d");
    public override ColorPalette PositivePalette => ColorPalette.FromHexBase("#43b030");
    public override ColorPalette NegativePalette => Palettes.Red; // I don't think this ever actually gets used.
    public override ColorPalette HighlightPalette => ColorPalette.FromHexBase("#beefc3");
}
