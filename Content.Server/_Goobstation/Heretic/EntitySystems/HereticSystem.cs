using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Chat.Systems;
using Content.Server.Heretic.Components;
using Content.Server.Objectives.Components;
using Content.Server.Store.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Nutrition;
using Content.Shared.Store.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly HereticKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    private float _timer;
    private float _passivePointCooldown = 20f * 60f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<HereticComponent, EventHereticAscension>(OnAscension);
        SubscribeLocalEvent<HereticComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<HereticComponent, DamageModifyEvent>(OnDamage);

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _timer += frameTime;

        if (_timer < _passivePointCooldown)
            return;

        _timer = 0f;

        var query = EntityQueryEnumerator<HereticComponent>();
        while (query.MoveNext(out var uid, out var heretic))
        {
            // passive point gain every 20 minutes
            UpdateKnowledge(uid, heretic, 1f);
        }
    }

    public void UpdateKnowledge(EntityUid uid, HereticComponent comp, float amount)
    {
        if (TryComp<StoreComponent>(uid, out var store))
        {
            _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { "KnowledgePoint", amount } }, uid, store);
            _store.UpdateUserInterface(uid, uid, store);
        }

        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        if (_mind.TryGetObjectiveComp<HereticKnowledgeConditionComponent>(mindId, out var objective, mind))
            objective.Researched += amount;
    }

    private void OnCompInit(Entity<HereticComponent> ent, ref ComponentInit args)
    {
        // add influence layer
        if (TryComp<EyeComponent>(ent, out var eye))
            _eye.SetVisibilityMask(ent, eye.VisibilityMask | EldritchInfluenceComponent.LayerMask);

        foreach (var knowledge in ent.Comp.BaseKnowledge)
        {
            _knowledge.AddKnowledge(ent, ent.Comp, knowledge);
        }
    }

    #region Internal events (target reroll, ascension, etc.)
    // notify the crew of how good the person is and play the cool sound :godo:
    private void OnAscension(Entity<HereticComponent> ent, ref EventHereticAscension args)
    {
        ent.Comp.Ascended = true;

        // how???
        if (ent.Comp.MainPath == null)
            return;

        var main = ent.Comp.MainPath.Value;
        if (!_prototypeManager.TryIndex<HereticPathPrototype>(main, out var pathPrototype))
        {
            return;
        }

        var ascendSound = pathPrototype.AscensionSound;
        var message = Loc.GetString(pathPrototype.Announcement);
        _chat.DispatchGlobalAnnouncement(message, Name(ent), true, ascendSound, Color.Pink);

        // leaving the old code here as an environmental storytelling corpse -kandi
        /*
        var pathLoc = ent.Comp.MainPath!.Value.Id.ToLower();
        var ascendSound = new SoundPathSpecifier($"/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/ascend_{pathLoc}.ogg");
        _chat.DispatchGlobalAnnouncement(Loc.GetString($"heretic-ascension-{pathLoc}"), Name(ent), true, ascendSound, Color.Pink);
        */

        // do other logic, e.g. make heretic immune to whatever
        switch (ent.Comp.MainPath.Value.Id)
        {
            case "Ash":
                RemComp<TemperatureComponent>(ent);
                RemComp<RespiratorComponent>(ent);
                RemComp<BarotraumaComponent>(ent);
                break;
        }
    }

    #endregion

    #region External events (damage, etc.)

    private void OnBeforeDamage(Entity<HereticComponent> ent, ref BeforeDamageChangedEvent args)
    {
        // ignore damage from heretic stuff
        if (args.Origin.HasValue && HasComp<HereticBladeComponent>(args.Origin))
            args.Cancelled = true;
    }
    private void OnDamage(Entity<HereticComponent> ent, ref DamageModifyEvent args)
    {
        if (!ent.Comp.Ascended)
            return;

        switch (ent.Comp.MainPath)
        {
            case "Ash":
                // nullify heat damage because zased
                args.Damage.DamageDict["Heat"] = 0;
                break;
        }
    }

    #endregion
}
