using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.Sheetlets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Impstation.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class ManifestSheetlet : Sheetlet<PalettedStylesheet>
{
    // using label sheetlet as base because this shit has no documentation
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var notoSansItalic10 = ResCache.GetFont("/Fonts/NotoSans/NotoSans-Italic.ttf", size: 10);

        return
        [
            E<RichTextLabel>()
                .Class(StyleClass.CrewManifestGender)
                .Prop("font", notoSansItalic10)
                .Prop("font-style", "italic"),
        ];
    }
}
