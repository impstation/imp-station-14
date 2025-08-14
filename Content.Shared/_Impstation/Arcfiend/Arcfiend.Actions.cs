using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcfiend;

[RegisterComponent, NetworkedComponent]
public sealed partial class ArcfiendActionComponent : Component
{
    [DataField] public float EnergyCost = 0;
}

public sealed partial class SapPowerEvent : EntityTargetActionEvent { }
public sealed partial class DischargeEvent : InstantActionEvent { }
public sealed partial class FlashEvent : InstantActionEvent { }
public sealed partial class ArcFlashEvent : EntityTargetActionEvent { }
public sealed partial class RideTheLightningEvent : InstantActionEvent { }
public sealed partial class JammingFieldEvent : InstantActionEvent { }
public sealed partial class JoltEvent : EntityTargetActionEvent { }
