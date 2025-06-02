using Content.Server.Body.Systems;
using Content.Server.Chat.Managers;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared._Impstation.Kodepiia;
using Content.Shared._Impstation.Pleebnar;
using Content.Shared._Impstation.Pleebnar.Components;
using Content.Shared.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.Gibbing.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Shared.Physics.Components;

namespace Content.Server._Impstation.Pleebnar;

public sealed class PleebnarGibSystem : SharedPleebnarGibSystem
{
    [Dependency] private readonly GibbingSystem _gibbingSystem = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PleebnarGibActionComponent, PleebnarGibEvent>(PleebnarGib);
        SubscribeLocalEvent<PleebnarGibActionComponent, PleebnarGibDoAfterEvent>(PleebnarGibDoafter);

    }

    public void PleebnarGib(Entity<PleebnarGibActionComponent> ent, ref PleebnarGibEvent args)
    {
        if (!HasComp<PleebnarGibbableComponent>(args.Target))
        {
            return;
        }
        if (!HasComp<BodyComponent>(args.Target))
        {
            return;
        }
        if (!TryComp<PhysicsComponent>(args.Target, out var targetPhysics))
            return;

        _popupSystem.PopupEntity(Loc.GetString("pleebnar-focus"),ent.Owner,PopupType.Small);
        var doargs = new DoAfterArgs(EntityManager, ent, targetPhysics.Mass/10, new SharedPleebnarGibSystem.PleebnarGibDoAfterEvent(), ent, args.Target)
        {
            DistanceThreshold = 1.5f,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };
        _doAfter.TryStartDoAfter(doargs);
        args.Handled = true;

    }

    public void PleebnarGibDoafter(Entity<PleebnarGibActionComponent> ent, ref PleebnarGibDoAfterEvent args)
    {
        if (args.Target == null)
        {
            return;
        }
        _body.GibBody((EntityUid)args.Target,true);
    }

}
