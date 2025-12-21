using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Robust.Shared.Utility; // EE THROWING

namespace Content.Shared.Damage.Systems;

public abstract class SharedDamageOtherOnHitSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DamageExamineSystem _damageExamine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOtherOnHitComponent, DamageExamineEvent>(OnDamageExamine);
        //SubscribeLocalEvent<DamageOtherOnHitComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow); // EE REMOVE
    }

    private void OnDamageExamine(Entity<DamageOtherOnHitComponent> ent, ref DamageExamineEvent args)
    {
        _damageExamine.AddDamageExamine(args.Message, _damageable.ApplyUniversalAllModifiers(ent.Comp.Damage * _damageable.UniversalThrownDamageModifier), Loc.GetString("damage-throw"));

        // EE START
        if (ent.Comp.StaminaCost == 0)
            return;

        var staminaCostMarkup = FormattedMessage.FromMarkupOrThrow(
            Loc.GetString("damage-stamina-cost",
            ("type", Loc.GetString("damage-throw")),
            ("cost", ent.Comp.StaminaCost)));
        args.Message.PushNewline();
        args.Message.AddMessage(staminaCostMarkup);
        // EE END
    }

    // EE REMOVE- we handle this in server DamageOtherOnHitSystem
    /* /// <summary>
    /// Prevent players with the Pacified status effect from throwing things that deal damage.
    /// </summary>
    private void OnAttemptPacifiedThrow(Entity<DamageOtherOnHitComponent> ent, ref AttemptPacifiedThrowEvent args)
    {
        args.Cancel("pacified-cannot-throw");
    } */
}
