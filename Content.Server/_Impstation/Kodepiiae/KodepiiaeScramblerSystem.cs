using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Kodepiiae;
using Content.Shared.Kodepiiae.Components;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Server._Impstation.Kodepiiae;

public sealed partial class KodepiiaeScramblerSystem : SharedKodepiiaeScramblerSystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KodepiiaeScramblerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<KodepiiaeScramblerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<KodepiiaeScramblerComponent, KodepiiaeScramblerEvent>(Scramble);
        SubscribeLocalEvent<KodepiiaeScramblerComponent, KodepiiaeScramblerDoAfterEvent>(OnScrambleDoAfter);
    }
    private void Scramble(Entity<KodepiiaeScramblerComponent> ent, ref KodepiiaeScramblerEvent args)
    {
        var doargs = new DoAfterArgs(EntityManager, ent, 4, new KodepiiaeScramblerDoAfterEvent(), ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };
        var popupOthers = Loc.GetString("kodepiiae-scramble-others", ("name", Identity.Entity(ent, EntityManager)), ("ent", ent));
        _popup.PopupEntity(popupOthers, ent, Filter.Pvs(ent).RemovePlayersByAttachedEntity(ent), true, PopupType.MediumCaution);
        _audio.PlayPvs(ent.Comp.ScramblerSound, ent);
        _doAfter.TryStartDoAfter(doargs);
        args.Handled = true;
    }

    private void OnScrambleDoAfter(Entity<KodepiiaeScramblerComponent> ent, ref KodepiiaeScramblerDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            _actionsSystem.SetCooldown(ent.Comp.ScramblerAction,TimeSpan.FromSeconds(10));
            return;
        }

        if (args.Handled)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return;
        var popupSelf = Loc.GetString("kodepiiae-scramble-self", ("name", Identity.Entity(ent, EntityManager)));
        _humanoidAppearance.LoadProfile(ent, HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species), humanoid);
        _popup.PopupEntity(popupSelf, ent, ent);
        args.Handled = true;
    }
}
