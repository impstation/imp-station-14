using Content.Shared._Impstation.Xenoarchaeology.Artifact.XAT.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for a xenoarch trigger that activates when a die is rolled with a certain value nearby.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATResurrectionSystem)), AutoGenerateComponentState]
public sealed partial class XATDiceRolledComponent : Component
{
    /// <summary>
    /// Range within which artifact going to listen to dice rolled event.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 12;

    /// <summary>
    /// Minimum number of sides the dice must have for the roll to be 'lucky' enough.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MinSides = 20;

    /// <summary>
    /// Target side that must be rolled for the roll to be 'lucky' enough.
    /// This is minimum side for 'high' mode (value scaled if rolled dice has more sides than MinSides)
    /// This is maximum side for 'low' mode (value scaled if rolled dice has more sides than MinSides)
    /// This is exact side for 'target' mode
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TargetSide = 20;

    /// <summary>
    /// How we determine if the roll is lucky
    /// "High": Roll >= TargetSide (values scaled if rolled dice has more sides than MinSides)
    /// "Low": Roll <= TargetSide (values scaled if rolled dice has more sides than MinSides)
    /// "Target": Roll = TargetSide
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Mode = "High";

}

