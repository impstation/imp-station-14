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


        if (data.ActionPrototypes != null)
        {
            foreach (var act in data.ActionPrototypes)
            {
                _action.AddAction(uid, act);
            }
        }

        // Manage Path Data
        if (GetKnowledgePath(data, out var path))
        {
            // set main path to knowledge's path if there is none and increase power
            if (comp.MainPath == null)
            {
                comp.MainPath = path;
                comp.Power += 1;
            }
            // If the knowledge is from main path, increase power by one
            else if (path== comp.MainPath)
            {
                comp.Power += 1;
            }
            // add path to sidepaths if knowledge isn't of the main path
            else
            {
                comp.SidePaths.Add(path);
            }
        }
        if (!silent)
            _popup.PopupEntity(Loc.GetString("heretic-knowledge-gain"), uid, uid);

        Dirty(uid, comp);
        comp.KnownKnowledge.Add(data);
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

        comp.KnownKnowledge.Remove(data);
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

    public bool GetKnowledgeRituals(ProtoId<HereticKnowledgePrototype> knowledge, [NotNullWhen(true)] out List<ProtoId<HereticRitualPrototype>>? rituals)
    {
        if (GetKnowledge(knowledge).RitualPrototypes != null)
        {
            rituals = GetKnowledge(knowledge).RitualPrototypes;
        }
        rituals = null;
        return rituals != null;
    }

    public bool HasKnowledge(HereticComponent comp, ProtoId<HereticKnowledgePrototype> knowledge)
    {
        return comp.KnownKnowledge.Contains(knowledge);
    }
    public List<ProtoId<HereticRitualPrototype>> AllKnownRituals(HereticComponent comp)
    {
        var rituals = new List<ProtoId<HereticRitualPrototype>>();
        foreach (var knowledge in comp.KnownKnowledge)
        {
            var ritualPrototypes = GetKnowledge(knowledge).RitualPrototypes;
            if (ritualPrototypes != null)
                rituals.AddRange(ritualPrototypes);
        }
        return rituals;
    }
}
