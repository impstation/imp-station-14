using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Starfall.Stylesheets.Lobby;

/// <summary>
/// Styles used exclusively by the Starfall trait editor.
/// </summary>
[CommonSheetlet]
public sealed class StarfallTraitsSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        // Aurora's Song - Match unselected cards to the WizDen palette.
        var traitCard = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#25252A"),
            BorderColor = sheet.SecondaryPalette.BackgroundLight,
            BorderThickness = new Thickness(1),
            ContentMarginLeftOverride = 6,
            ContentMarginTopOverride = 4,
            ContentMarginRightOverride = 6,
            ContentMarginBottomOverride = 4,
        };

        // Aurora's Song - Match selected cards to the WizDen palette.
        var selectedTraitCard = new StyleBoxFlat
        {
            BackgroundColor = sheet.PrimaryPalette.BackgroundLight,
            BorderColor = sheet.PrimaryPalette.Element,
            BorderThickness = new Thickness(1),
            ContentMarginLeftOverride = 6,
            ContentMarginTopOverride = 4,
            ContentMarginRightOverride = 6,
            ContentMarginBottomOverride = 4,
        };

        // Aurora's Song - Use a dark WizDen-style category header.
        var header = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#202126"),
            BorderColor = sheet.HighlightPalette.Base,
            BorderThickness = new Thickness(0, 0, 0, 2),
            ContentMarginLeftOverride = 8,
            ContentMarginTopOverride = 5,
            ContentMarginRightOverride = 8,
            ContentMarginBottomOverride = 5,
        };

        return
        [
            // Aurora's Song - Trait-specific category header styles.
            E<PanelContainer>()
                .Class("StarfallTraitHeader")
                .Panel(header),

            // Aurora's Song - Trait-specific category title style.
            E<Label>()
                .Class("StarfallTraitTitle")
                .Font(sheet.BaseFont.GetFont(15, FontKind.Bold))
                .FontColor(sheet.HighlightPalette.Text),

            E<PanelContainer>()
                .Class("StarfallTraitCard")
                .Panel(traitCard),

            E<PanelContainer>()
                .Class("StarfallTraitCard")
                .Class("StarfallTraitSelected")
                .Panel(selectedTraitCard),

            // Aurora's Song - Trait costs use the active palette instead of hard-coded colors.
            E<Label>()
                .Class("StarfallTraitCostPositive")
                .FontColor(sheet.NegativePalette.Text),

            E<Label>()
                .Class("StarfallTraitCostNegative")
                .FontColor(sheet.PositivePalette.Text),

            E<Label>()
                .Class("StarfallTraitCostNeutral")
                .FontColor(sheet.SecondaryPalette.TextDark),
        ];
    }
}
