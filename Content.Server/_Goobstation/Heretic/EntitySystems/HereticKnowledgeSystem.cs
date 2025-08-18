using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Heretic;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Shared._Goobstation.Heretic.Components;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticKnowledgeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly HereticRitualSystem _ritual = default!;

    public HereticKnowledgePrototype GetKnowledge(ProtoId<HereticKnowledgePrototype> id)
    {
        return _proto.Index(id);
    }

    public void AddKnowledge(EntityUid uid, HereticComponent comp, ProtoId<HereticKnowledgePrototype> id, bool silent = true)
    {
        var data = GetKnowledge(id);

        if (data.Event != null)
            RaiseLocalEvent(uid, (object) data.Event, true);

        if (data.ActionPrototypes != null && data.ActionPrototypes.Count > 0)
        {
            foreach (var act in data.ActionPrototypes)
            {
                _action.AddAction(uid, act);
            }
        }

        if (data.RitualPrototypes != null && data.RitualPrototypes.Count > 0)
        {
            foreach (var ritual in data.RitualPrototypes)
            {
                comp.KnownRituals.Add(_ritual.GetRitual(ritual));
            }
        }

        Dirty(uid, comp);

        // Manage Path Data
        if (GetKnowledgePath(data, out var path))
        {
            // set main path to knowledge's path if there is none
            if (comp.MainPath == null)
            {
                comp.MainPath = path;
            }
            // If the knowledge is from main path, increase path stage to value
            if (data.Stage > comp.PathStage && path== comp.MainPath)
            {
                comp.PathStage = data.Stage;
            }
            // add path to sidepaths if knowledge is of the main path
            if (comp.MainPath != path)
            {
                comp.SidePaths.Add(path);
            }
        }

        if (!silent)
            _popup.PopupEntity(Loc.GetString("heretic-knowledge-gain"), uid, uid);
    }
    public void RemoveKnowledge(EntityUid uid, HereticComponent comp, ProtoId<HereticKnowledgePrototype> id, bool silent = false)
    {
        var data = GetKnowledge(id);

        if (data.ActionPrototypes != null && data.ActionPrototypes.Count > 0)
        {
            foreach (var act in data.ActionPrototypes)
            {
                var actionName = _proto.Index<EntityPrototype>(act);
                // jesus christ.
                foreach (var action in _action.GetActions(uid))
                {
                    if (Name(action.Owner) == actionName.Name)
                        _action.RemoveAction(action.Owner);
                }
            }
        }

        if (data.RitualPrototypes != null && data.RitualPrototypes.Count > 0)
        {
            foreach (var ritual in data.RitualPrototypes)
            {
                comp.KnownRituals.Remove(_ritual.GetRitual(ritual));
            }
        }

        Dirty(uid, comp);

        if (!silent)
            _popup.PopupEntity(Loc.GetString("heretic-knowledge-loss"), uid, uid);
    }

    public bool GetKnowledgePath(ProtoId<HereticKnowledgePrototype> knowledge, [NotNullWhen(true)] out HereticPathPrototype? path)
    {
        var paths = _proto.EnumeratePrototypes<HereticPathPrototype>().ToList();
        foreach (var protoPath in paths)
        {
            foreach (var protoKnowledge in protoPath.Knowledge)
            {
                if (knowledge != protoKnowledge)
                {
                    continue;
                }
                path = protoPath;
                return true;
            }
        }
        path = null;
        return false;
    }
}
