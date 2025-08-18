using Content.Shared.Heretic.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Heretic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticComponent : Component
{
    #region Prototypes

    [DataField]
    public List<ProtoId<HereticKnowledgePrototype>> BaseKnowledge = new()
    {
        "BreakOfDawn",
        "HeartbeatOfMansus",
        "AmberFocus",
        "CodexCicatrix",
    };

    #endregion

    [DataField, AutoNetworkedField] public List<ProtoId<HereticRitualPrototype>> KnownRituals = new();
    [DataField] public ProtoId<HereticRitualPrototype>? ChosenRitual;

    /// <summary>
    ///     The main path the heretic is on.
    /// </summary>
    [DataField]
    public ProtoId<HereticPathPrototype>? MainPath;

    /// <summary>
    ///     All side paths the heretic is on.
    /// </summary>
    [DataField]
    public List<ProtoId<HereticPathPrototype>> SidePaths = [];

    /// <summary>
    ///     Indicates a stage of a path the heretic is on. 0 is no path, 10 is ascension
    /// </summary>
    [DataField, AutoNetworkedField] public int PathStage;

    [DataField, AutoNetworkedField] public bool Ascended;

    /// <summary>
    ///     Used to prevent double casting mansus grasp.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public bool MansusGraspActive = false;

    public List<ProtoId<HereticPathPrototype>> AllPaths()
    {
        var paths = new List<ProtoId<HereticPathPrototype>>();
        paths.AddRange(SidePaths);
        if (MainPath != null)
            paths.Add(MainPath.Value);
        return paths;
    }
}
