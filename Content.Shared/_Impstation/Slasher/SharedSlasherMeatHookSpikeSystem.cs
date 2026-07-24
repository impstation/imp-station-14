using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared._Impstation.Slasher.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Slasher;

/// <summary>
/// Shared hook, unhook, and immobilization behavior for Slasher meathooks.
/// </summary>
public sealed class SharedSlasherMeatHookSpikeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ISharedAdminLogManager _logger = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    /// <summary>
    /// Registers shared hook interaction, do-after, movement restriction, and verb handlers.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlasherMeatHookSpikeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SlasherMeatHookSpikeComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<SlasherMeatHookSpikeComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<SlasherMeatHookSpikeComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);
        SubscribeLocalEvent<SlasherMeatHookSpikeComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<SlasherMeatHookSpikeComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SlasherMeatHookSpikeComponent, CanDropTargetEvent>(OnCanDrop);
        SubscribeLocalEvent<SlasherMeatHookSpikeComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<SlasherMeatHookSpikeComponent, SlasherSpikeHookDoAfterEvent>(OnSpikeHookDoAfter);
        SubscribeLocalEvent<SlasherMeatHookSpikeComponent, SlasherSpikeUnhookDoAfterEvent>(OnSpikeUnhookDoAfter);
        SubscribeLocalEvent<SlasherMeatHookSpikeComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<SlasherMeatHookSpikeComponent, DestructionEventArgs>(OnDestruction);

        SubscribeLocalEvent<SlasherMeatHookedComponent, ChangeDirectionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<SlasherMeatHookedComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<SlasherMeatHookedComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<SlasherMeatHookedComponent, DropAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<SlasherMeatHookedComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<SlasherMeatHookedComponent, PickupAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<SlasherMeatHookedComponent, IsEquippingAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<SlasherMeatHookedComponent, IsUnequippingAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<SlasherMeatHookedComponent, AccessibleOverrideEvent>(OnAccessibleOverride);
    }

    /// <summary>
    /// Ensures the hook body container exists on component initialization.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="args">Component initialization event data.</param>
    private void OnInit(Entity<SlasherMeatHookSpikeComponent> ent, ref ComponentInit args)
    {
        ent.Comp.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
    }

    /// <summary>
    /// Validates insertion targets for the hook container.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="args">Container insertion attempt event data.</param>
    private void OnInsertAttempt(Entity<SlasherMeatHookSpikeComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<HumanoidAppearanceComponent>(args.EntityUid) && IsEligibleHookTarget(args.EntityUid))
            return;

        args.Cancel();
    }

    /// <summary>
    /// Applies hook effects when a victim is inserted into the hook container.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="args">Container insertion event data.</param>
    private void OnEntInsertedIntoContainer(Entity<SlasherMeatHookSpikeComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_gameTiming.ApplyingState)
            return;

        EnsureComp<SlasherMeatHookedComponent>(args.Entity);
        _damageableSystem.TryChangeDamage(args.Entity, ent.Comp.SpikeDamage, true);
    }

    /// <summary>
    /// Removes hook effects when a victim is removed from the hook container.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="args">Container removal event data.</param>
    private void OnEntRemovedFromContainer(Entity<SlasherMeatHookSpikeComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_gameTiming.ApplyingState)
            return;

        RemComp<SlasherMeatHookedComponent>(args.Entity);
        _damageableSystem.TryChangeDamage(args.Entity, ent.Comp.SpikeDamage, true);
    }

    /// <summary>
    /// Handles drag-drop checks against the hook container.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="args">Drop-target validation event data.</param>
    private void OnCanDrop(Entity<SlasherMeatHookSpikeComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = _containerSystem.CanInsert(args.Dragged, ent.Comp.BodyContainer)
            && IsEligibleHookTarget(args.Dragged);
        args.Handled = true;
    }

    /// <summary>
    /// Starts hook do-after flow when a user drag-drops an entity onto the hook.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="args">Drag-drop interaction event data.</param>
    private void OnDragDrop(Entity<SlasherMeatHookSpikeComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!IsEligibleHookTarget(args.Dragged))
        {
            _popupSystem.PopupClient(Loc.GetString("slasher-meathook-hook-invalid-target"), ent, args.User, PopupType.MediumCaution);
            args.Handled = true;
            return;
        }

        ShowPopups("comp-kitchen-spike-begin-hook-self",
            "comp-kitchen-spike-begin-hook-self-other",
            "comp-kitchen-spike-begin-hook-other-self",
            "comp-kitchen-spike-begin-hook-other",
            args.User,
            args.Dragged,
            ent);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.HookDelay,
            new SlasherSpikeHookDoAfterEvent(),
            ent,
            target: args.Dragged)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        });

        args.Handled = true;
    }

    /// <summary>
    /// Completes hook do-after and inserts target into the hook container when valid.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="args">Hook do-after completion event data.</param>
    private void OnSpikeHookDoAfter(Entity<SlasherMeatHookSpikeComponent> ent, ref SlasherSpikeHookDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !args.Target.HasValue)
            return;

        if (!IsEligibleHookTarget(args.Target.Value))
        {
            _popupSystem.PopupClient(Loc.GetString("slasher-meathook-hook-invalid-target"), ent, args.User, PopupType.MediumCaution);
            args.Handled = true;
            return;
        }

        if (_containerSystem.Insert(args.Target.Value, ent.Comp.BodyContainer))
        {
            ShowPopups("comp-kitchen-spike-hook-self",
                "comp-kitchen-spike-hook-self-other",
                "comp-kitchen-spike-hook-other-self",
                "comp-kitchen-spike-hook-other",
                args.User,
                args.Target.Value,
                ent);

            var logSeverity = HasComp<HumanoidAppearanceComponent>(args.Target) ? LogImpact.High : LogImpact.Medium;
            _logger.Add(LogType.Action,
                logSeverity,
                $"{ToPrettyString(args.User):user} put {ToPrettyString(args.Target):target} on the {ToPrettyString(ent):spike}");

            _audioSystem.PlayPredicted(ent.Comp.SpikeSound, ent, args.User);
        }

        args.Handled = true;
    }

    /// <summary>
    /// Completes unhook do-after and removes target from the hook container when valid.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="args">Unhook do-after completion event data.</param>
    private void OnSpikeUnhookDoAfter(Entity<SlasherMeatHookSpikeComponent> ent, ref SlasherSpikeUnhookDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !args.Target.HasValue)
            return;

        if (_containerSystem.Remove(args.Target.Value, ent.Comp.BodyContainer))
        {
            ShowPopups("comp-kitchen-spike-unhook-self",
                "comp-kitchen-spike-unhook-self-other",
                "comp-kitchen-spike-unhook-other-self",
                "comp-kitchen-spike-unhook-other",
                args.User,
                args.Target.Value,
                ent);

            _logger.Add(LogType.Action,
                LogImpact.Medium,
                $"{ToPrettyString(args.User):user} took {ToPrettyString(args.Target):target} off the {ToPrettyString(ent):spike}");

            _audioSystem.PlayPredicted(ent.Comp.SpikeSound, ent, args.User);
        }

        args.Handled = true;
    }

    /// <summary>
    /// Handles empty-hand interaction with the hook to start unhook attempts.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="args">Hand interaction event data.</param>
    private void OnInteractHand(Entity<SlasherMeatHookSpikeComponent> ent, ref InteractHandEvent args)
    {
        // Let the slasher-specific server hook system handle primary click harvest interactions.
        if (HasComp<SlasherRoleComponent>(args.User)
            && HasComp<SlasherMeatHookComponent>(ent)
            && TryGetContainedVictim(ent, out _))
        {
            return;
        }

        if (args.Handled || !TryGetUnhookVictim(ent, args.User, out var victim))
            return;

        TryStartUnhook(ent, args.User, victim);
        args.Handled = true;
    }

    /// <summary>
    /// Handles using-item interaction with the hook and shows slasher-specific unhook restrictions.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="args">Using-item interaction event data.</param>
    private void OnInteractUsing(Entity<SlasherMeatHookSpikeComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryGetContainedVictim(ent, out _))
            return;

        _popupSystem.PopupClient(Loc.GetString("slasher-meathook-unhook-left-click"), ent, args.User, PopupType.Medium);
        args.Handled = true;
    }

    /// <summary>
    /// Adds available interaction verbs for unhooking when allowed.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="args">Verb request event data.</param>
    private void OnGetVerbs(Entity<SlasherMeatHookSpikeComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!TryGetUnhookVictim(ent, args.User, out var victim))
            return;

        var user = args.User;
        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("comp-kitchen-spike-unhook-verb"),
            Act = () => TryStartUnhook(ent, user, victim),
            Impact = LogImpact.Medium,
        });
    }

    /// <summary>
    /// Cancels restricted actions for entities immobilized on a hook.
    /// </summary>
    /// <param name="uid">Entity with hook immobilization marker.</param>
    /// <param name="component">Immobilization marker component.</param>
    /// <param name="args">Cancellable action event data.</param>
    private static void OnAttempt(EntityUid uid, SlasherMeatHookedComponent component, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    /// <summary>
    /// Allows access checks against the containing hook for immobilized victims when appropriate.
    /// </summary>
    /// <param name="ent">Entity with hook immobilization marker.</param>
    /// <param name="args">Accessibility override event data.</param>
    private void OnAccessibleOverride(Entity<SlasherMeatHookedComponent> ent, ref AccessibleOverrideEvent args)
    {
        if (args.Accessible || args.Target != ent.Owner)
            return;

        var xform = Transform(ent);
        if (!_interaction.CanAccess(args.User, xform.ParentUid))
            return;

        args.Accessible = true;
        args.Handled = true;
    }

    /// <summary>
    /// Empties the hook container when the hook is destroyed.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="args">Destruction event data.</param>
    private void OnDestruction(Entity<SlasherMeatHookSpikeComponent> ent, ref DestructionEventArgs args)
    {
        _containerSystem.EmptyContainer(ent.Comp.BodyContainer, destination: Transform(ent).Coordinates);
    }

    /// <summary>
    /// Shows localized predicted popup messages for hook and unhook actions.
    /// </summary>
    /// <param name="selfLocMessageSelf">Localization key for self-action shown to user.</param>
    /// <param name="selfLocMessageOthers">Localization key for self-action shown to observers.</param>
    /// <param name="locMessageSelf">Localization key for action-on-other shown to user.</param>
    /// <param name="locMessageOthers">Localization key for action-on-other shown to observers.</param>
    /// <param name="user">Acting user.</param>
    /// <param name="victim">Victim being hooked or unhooked.</param>
    /// <param name="hook">Hook entity involved in the action.</param>
    private void ShowPopups(string selfLocMessageSelf,
        string selfLocMessageOthers,
        string locMessageSelf,
        string locMessageOthers,
        EntityUid user,
        EntityUid victim,
        EntityUid hook)
    {
        string messageSelf;
        string messageOthers;

        var victimIdentity = Identity.Entity(victim, EntityManager);
        if (user == victim)
        {
            messageSelf = Loc.GetString(selfLocMessageSelf, ("hook", hook));
            messageOthers = Loc.GetString(selfLocMessageOthers, ("victim", victimIdentity), ("hook", hook));
        }
        else
        {
            messageSelf = Loc.GetString(locMessageSelf, ("victim", victimIdentity), ("hook", hook));
            messageOthers = Loc.GetString(locMessageOthers,
                ("user", Identity.Entity(user, EntityManager)),
                ("victim", victimIdentity),
                ("hook", hook));
        }

        _popupSystem.PopupPredicted(messageSelf, messageOthers, hook, user, PopupType.MediumCaution);
    }

    /// <summary>
    /// Tries to get a removable victim from the hook for the given user.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="user">User attempting unhook.</param>
    /// <param name="victim">Resolved victim when successful.</param>
    /// <returns>True when a valid victim can be unhooked by the user.</returns>
    private bool TryGetUnhookVictim(Entity<SlasherMeatHookSpikeComponent> ent, EntityUid user, out EntityUid victim)
    {
        if (!TryGetContainedVictim(ent, out victim))
            return false;

        return _containerSystem.CanRemove(victim, ent.Comp.BodyContainer);
    }

    /// <summary>
    /// Valid hook targets are critical victims or recently dead victims.
    /// </summary>
    private bool IsEligibleHookTarget(EntityUid target)
    {
        if (!TryComp<MobStateComponent>(target, out var mobState))
            return false;

        if (mobState.CurrentState == MobState.Critical)
            return true;

        if (mobState.CurrentState != MobState.Dead)
            return false;

        if (!TryComp<SlasherRecentDeathComponent>(target, out var recentDeath) || recentDeath.TimeOfDeath == null)
            return false;

        return _gameTiming.CurTime - recentDeath.TimeOfDeath.Value <= recentDeath.RecentDeathWindow;
    }

    /// <summary>
    /// Retrieves the currently contained victim from the hook container.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="victim">Resolved victim when present.</param>
    /// <returns>True when a victim is currently contained.</returns>
    private static bool TryGetContainedVictim(Entity<SlasherMeatHookSpikeComponent> ent, out EntityUid victim)
    {
        var contained = ent.Comp.BodyContainer.ContainedEntity;
        if (!contained.HasValue)
        {
            victim = default;
            return false;
        }

        victim = contained.Value;
        return true;
    }

    /// <summary>
    /// Starts unhook do-after flow with the proper delay rules for user and victim context.
    /// </summary>
    /// <param name="ent">Hook entity and spike component data.</param>
    /// <param name="user">User attempting unhook.</param>
    /// <param name="victim">Victim to unhook.</param>
    private void TryStartUnhook(Entity<SlasherMeatHookSpikeComponent> ent, EntityUid user, EntityUid victim)
    {
        if (!_containerSystem.CanRemove(victim, ent.Comp.BodyContainer))
            return;

        var delay = ent.Comp.UnhookDelay;
        if (user == victim && TryComp<SlasherMeatHookComponent>(ent, out var slasherHook))
            delay = slasherHook.SelfUnhookDelay;

        ShowPopups("comp-kitchen-spike-begin-unhook-self",
            "comp-kitchen-spike-begin-unhook-self-other",
            "comp-kitchen-spike-begin-unhook-other-self",
            "comp-kitchen-spike-begin-unhook-other",
            user,
            victim,
            ent);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            delay,
            new SlasherSpikeUnhookDoAfterEvent(),
            ent,
            target: victim)
        {
            BreakOnDamage = user != victim,
            BreakOnMove = true,
        });
    }


}
