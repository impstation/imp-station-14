using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class SOSCartridgeComponent : Component
{
    //Path to the id container
    public const string PDAIdContainer = "PDA-id";

    //Timeout between calls
    public const float TimeOut = 30;

    /// Sound played when SOS is activated, audible to nearby players.
    [DataField]
    public SoundSpecifier ActivationSound = new SoundPathSpecifier("/Audio/_Impstation/Items/SOS.ogg");

    /// hehehe
    [DataField]
    public SoundSpecifier TomActivationSound = new SoundPathSpecifier("/Audio/_Impstation/Misc/tomscream.ogg");

    /// :)
    [DataField]
    public float TomSoundChance = 0.01f;

    [DataField]
    //Name to use if no id is found
    public string DefaultName = "sos-caller-defaultname";

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedDefaultName => Loc.GetString(DefaultName);

    [DataField]
    //Notification message
    public string HelpMessage = "sos-message";

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedHelpMessage => Loc.GetString(HelpMessage);

    [DataField]
    //Channel to notify
    public ProtoId<RadioChannelPrototype> HelpChannel = "Security";

    [DataField]
    //Countdown until next call is allowed
    public float Timer = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool CanCall => Timer <= 0;
}
