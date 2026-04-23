using Content.Shared._Impstation.Sneezing;
using Content.Shared._Impstation.Body.Components;
using Content.Shared._Impstation.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Fluids;
using Content.Shared.Forensics.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Impstation.Medical;
public sealed class SneezeSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedForensicsSystem _forensics = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SneezingComponent, TrySneezeEvent>(TryBodySneezeSolution);
    }

    private void TryBodySneezeSolution(Entity<SneezingComponent> ent, ref TrySneezeEvent args)
    {
        if (args.Handled)
            return;

        // Requirement: must have a nose
        var noseList = _body.GetBodyOrganEntityComps<NoseComponent>((ent, null));
        if (noseList.Count == 0)
            return;

        // Transfer any stored nose solution (optional, if you kept it)
        foreach (var nose in noseList)
        {
            if (_solutionContainer.ResolveSolution(nose.Owner, NoseSystem.DefaultSolutionName, ref nose.Comp1.Solution, out var sol))
                _solutionContainer.TryTransferSolution(nose.Comp1.Solution.Value, args.Sol, sol.AvailableVolume);
        }

        args.Handled = true;
    }

    /// <summary>
    /// Makes an entity sneeze.
    /// </summary>
    public void Sneeze(EntityUid uid, bool force = false, ProtoId<ReagentPrototype>? overridePrototype = null)
    {
        // Do not sneeze if dead (unless forced)
        if (!force && _mobState.IsDead(uid))
            return;

        var solution = new Solution();

        var ev = new TrySneezeEvent(solution, force);
        RaiseLocalEvent(uid, ref ev);

        if (!ev.Handled)
            return;

        if (!TryComp<SneezingComponent>(uid, out var comp))
            return;

        var sneezeSound = new SoundCollectionSpecifier(
            comp.SneezeCollection,
            AudioParams.Default.WithVariation(0.2f).WithVolume(-4f));

        var sniffleSound = new SoundCollectionSpecifier(
            comp.SniffleCollection,
            AudioParams.Default.WithVariation(0.2f).WithVolume(-4f));

        var mucusPrototype = overridePrototype ?? comp.MucusPrototype;

        solution.AddReagent(new ReagentId(mucusPrototype.Value), 1f);
        solution.AddReagent(new ReagentId("Germs"), 0.25f);

        // Spill onto floor
        if (_puddle.TrySpillAt(uid, solution, out var puddle, false))
        {
            _forensics.TransferDna(puddle, uid, false);
        }

        if (!_netManager.IsServer)
            return;

        // Play sneeze sound + popup
        _audio.PlayPvs(sneezeSound, uid);
        _popup.PopupEntity(Loc.GetString("disease-sneeze", ("person", Identity.Entity(uid, EntityManager))), uid);

        // 🎲 Random sniffle chance
        if (_random.Prob(comp.SniffleChance))
        {
            _audio.PlayPvs(sniffleSound, uid);
            _popup.PopupEntity(Loc.GetString("disease-sniffle", ("person", Identity.Entity(uid, EntityManager))), uid);
        }
    }
}

[ByRefEvent]
public record struct TrySneezeEvent(Solution Sol, bool Forced = false, bool Handled = false);
