using Content.Shared.Heretic.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Heretic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticComponent : Component
{
    [DataField]
    public List<ProtoId<HereticKnowledgePrototype>> BaseKnowledge = new()
    {
        "BreakOfDawn",
        "HeartbeatOfMansus",
        "AmberFocus",
        "CodexCicatrix",
    };

    [DataField] public ProtoId<HereticRitualPrototype>? ChosenRitual;

    /// <summary>
    ///     All knowledge the heretic knows.
    /// </summary>
    [DataField, AutoNetworkedField] public List<ProtoId<HereticKnowledgePrototype>> KnownKnowledge = [];

    /// <summary>
    ///     The main path the heretic is on.
    /// </summary>
    [DataField]
    public ProtoId<HereticPathPrototype>? MainPath;

    /// <summary>
    ///     Indicates the power level of a heretic.
    /// </summary>
    [DataField, AutoNetworkedField] public int Power;

    /// <summary>
    ///     All side paths the heretic is on.
    /// </summary>
    [DataField]
    public List<ProtoId<HereticPathPrototype>> SidePaths = [];

    [DataField, AutoNetworkedField] public bool Ascended;

    public List<ProtoId<HereticPathPrototype>> AllPaths()
    {
        var paths = new List<ProtoId<HereticPathPrototype>>();
        paths.AddRange(SidePaths);
        if (MainPath != null)
            paths.Add(MainPath.Value);
        return paths;
    }
}
