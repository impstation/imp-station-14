using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.BloodlessChimp.Components;
[RegisterComponent]

public sealed partial class DropItemOnDamageComponent :  Component
{
    [DataField("itemToDrop"), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId ItemToDrop = new EntProtoId();

    [DataField("dropChance"), ViewVariables(VVAccess.ReadWrite)]
    public float DropChance = 1f;

    [DataField("dropOne"), ViewVariables(VVAccess.ReadWrite)]
    public bool DropOne = false;

    /// <summary>
    /// The entitys' states that passive damage will apply in
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<MobState> AllowedStates = new();

    /// <summary>
    /// Damage / Healing per interval dealt to the entity every interval
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier requiredTypes = new();
}
