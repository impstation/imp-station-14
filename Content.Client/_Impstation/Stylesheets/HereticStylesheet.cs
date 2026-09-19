using System.Linq;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client._Impstation.Stylesheets;

/// <summary>
/// Stylesheet for heretic-themed UIs.
/// </summary>
[Virtual]
public partial class HereticStylesheet : CommonStylesheet
{
    public override string StylesheetName => "Heretic";

    public override NotoFontFamilyStack BaseFont { get; } // Upstream TODO about NotoFontFamilyStack being temporary.

    public override Dictionary<Type, ResPath[]> Roots => new()
    {
        { typeof(TextureResource), [] },
    };

    private const int PrimaryFontSize = 12;
    private const int FontSizeStep = 2;

    // why? see InterfaceStylesheet.cs
    // ReSharper disable once UseCollectionExpression
    private readonly List<(string?, int)> _commonFontSizes = new()
    {
        (null, PrimaryFontSize),
        (StyleClass.FontSmall, PrimaryFontSize - FontSizeStep),
        (StyleClass.FontLarge, PrimaryFontSize + FontSizeStep),
    };

    public HereticStylesheet(object config, StylesheetManager man) : base(config)
    {
        BaseFont = new NotoFontFamilyStack(ResCache);
        var rules = new[]
        {
            // Set up important rules that need to go first.
            GetRulesForFont(null, BaseFont, _commonFontSizes),
            // Set up our core rules.
            [
                // Declare the default font.
                Element().Prop(Label.StylePropertyFont, BaseFont.GetFont(PrimaryFontSize)),
            ],
            // Finally, load all the other sheetlets.
            GetAllSheetletRules<PalettedStylesheet, CommonSheetletAttribute>(man),
            GetAllSheetletRules<HereticStylesheet, CommonSheetletAttribute>(man),
        };

        Stylesheet = new Stylesheet(rules.SelectMany(x => x).ToArray());
    }
}
