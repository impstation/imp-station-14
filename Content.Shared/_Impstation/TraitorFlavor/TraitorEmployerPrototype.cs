using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.TraitorFlavor;

[Prototype]
public sealed partial class TraitorEmployerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = "The Syndicate";

    [DataField]
    public Color Color = Color.Crimson;

    [DataField]
    public LocId IntroText;

    [DataField]
    public LocId GoalText;

    [DataField]
    public LocId AlliesText;

    [DataField]
    public LocId UplinkText;

    [DataField]
    public LocId RoundendText;
}
