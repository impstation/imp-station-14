using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Content.Shared.Emag.Systems;
using Content.Server.Speech.Components;


namespace Content.Server._Impstation.Toys;

public sealed class FuzzboSystem : EntitySystem
{

    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FuzzboComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<FuzzboComponent, InteractUsingEvent>(OnInteractUsing);
    }

    /// <summary>
    /// Makes an eating noise play when keys are inserted.
    /// </summary>
    private void OnInteractUsing(EntityUid uid, FuzzboComponent component, InteractUsingEvent args)
    {
        if (HasComp<FuzzboComponent>(args.Used))
        {
            args.Handled = true;
            TryInsertKey(uid, component, args);
        }
    }

    private void TryInsertKey(EntityUid uid, FuzzboComponent component, InteractUsingEvent args)
    {
        if (_container.Insert(args.Used, component.KeyContainer))
        {
            _audio.PlayPredicted(component.KeyInsertionSound, uid, uid);
            args.Handled = true;
            return;
        }
    }

    /// <summary>
    /// Allows the player inhabiting the ghost role to activate Harm Mode at will, removes Relentless Positivity accent.
    /// </summary>
    private void OnEmagged(EntityUid uid, FuzzboComponent component, ref GotEmaggedEvent args)
    {
        {
            if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
                return;

            if (_emag.CheckFlag(uid, EmagType.Interaction))
                return;

            args.Handled = true;
        }

        {
            EnsureComp<CombatModeComponent>(uid);
        }

        {
            RemComp<RelentlessPositivityComponent>(uid);
        }
    }

}
