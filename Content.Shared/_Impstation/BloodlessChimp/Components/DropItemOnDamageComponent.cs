using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.BloodlessChimp.Components;
[RegisterComponent]
public sealed partial class DropItemOnDamageComponent :  Component
{
    public EntProtoId ItemToDrop = new EntProtoId();

    public float DropChance;

    /// <summary>
    /// The entitys' states that passive damage will apply in
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<MobState> AllowedStates = new();

    /// <summary>
    /// Damage / Healing per interval dealt to the entity every interval
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier allowedDamageTypes = new();
}
