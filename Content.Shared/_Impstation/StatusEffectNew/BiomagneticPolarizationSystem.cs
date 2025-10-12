
using Content.Shared.Lightning;
using Content.Shared.StatusEffectNew;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.StatusEffectNew;

public abstract class SharedBiomagneticPolarizationSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;

    private readonly EntProtoId _effectID = "StatusEffectBiomagneticPolarization";

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Returns (TRUE, FALSE) if collision occurs between two entities of opposite polarity.
    /// Returns (FALSE, TRUE) if collision occurs between two entities of the same polarity.
    /// Returns (FALSE, FALSE) if no valid collision is detected.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="biomagComp"></param>
    /// <param name="frameTime"></param>
    /// <returns></returns>
    protected (bool, bool) HandleCollisions(Entity<PhysicsComponent>? entPhys, BiomagneticPolarizationStatusEffectComponent biomagComp)
    {
        if (entPhys is not { } ent)
            return (false, false);

        var fieldDispersed = false;
        var triggeredCooldown = false;
        var physComp = ent.Comp;

        if (physComp.ContactCount == 0)
            return (fieldDispersed, triggeredCooldown);

        var xform = Transform(ent.Owner);

        if (xform.ParentUid != xform.GridUid && xform.ParentUid != xform.MapUid)
            return (fieldDispersed, triggeredCooldown);

        var contacts = _physics.GetContacts(ent.Owner);
        while (contacts.MoveNext(out var contact))
        {
            if (!contact.IsTouching)
                continue;

            var ourFixture = contact.OurFixture(ent.Owner);

            if (ourFixture.Id != biomagComp.FixtureId)
                continue;

            var other = contact.OtherEnt(ent.Owner);

            if (!_statusEffect.TryGetStatusEffect(other, _effectID, out var otherEffectEnt) || !TryComp<PhysicsComponent>(other, out var otherPhysics))
                continue;

            if (!TryComp<BiomagneticPolarizationStatusEffectComponent>(otherEffectEnt, out var otherBiomagComp))
                continue;

            // if two people with the same charge come in contact, they repel one another
            if (biomagComp.Polarization == otherBiomagComp.Polarization)
            {
                triggeredCooldown = true;
                var xformQuery = GetEntityQuery<TransformComponent>();
                var worldPos = _xform.GetWorldPosition(xform, xformQuery);

                var otherXform = Transform(other);
                var direction = _xform.GetWorldPosition(otherXform, xformQuery) - worldPos;

                var strengthAverage = (biomagComp.CurrentStrength + otherBiomagComp.CurrentStrength) / 2;
                _throw.TryThrow(ent, direction * -strengthAverage, strengthAverage * biomagComp.ThrowStrengthMult);
                _throw.TryThrow(other, direction * strengthAverage, strengthAverage * otherBiomagComp.ThrowStrengthMult);
            }
            // if two people with DIFFERENT charge come in contact, they cause an explosion and lose their charge.
            else if (biomagComp.Polarization != otherBiomagComp.Polarization)
            {
                fieldDispersed = true;
                otherBiomagComp.Expired = true;
            }
        }
        return (fieldDispersed, triggeredCooldown);
    }
}
