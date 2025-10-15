using Content.Shared.StatusEffectNew;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.StatusEffectNew;

public abstract class SharedBiomagneticPolarizationSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
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
    /// The third bool value returns TRUE if either involved entity was at strength cap.
    /// </summary>
    protected (bool Opposite, bool Same, bool StrCapInvolved) HandleCollisions(Entity<PhysicsComponent?>? entPhys, BiomagneticPolarizationStatusEffectComponent biomagComp)
    {
        if (entPhys is not { } ent || ent.Comp is not { } physComp || physComp.ContactCount == 0)
            return (false, false, false);

        var xform = Transform(ent.Owner);

        if (xform.ParentUid != xform.GridUid && xform.ParentUid != xform.MapUid)
            return (false, false, false);

        var samePolarityCollision = false;
        var oppositePolarityCollision = false;
        var strengthCapInvolved = false;

        var contacts = _physics.GetContacts(ent.Owner);
        while (contacts.MoveNext(out var contact))
        {
            if (!contact.IsTouching)
                continue;

            var ourFixture = contact.OurFixture(ent.Owner);

            if (ourFixture.Id != biomagComp.FixtureId)
                continue;

            var other = contact.OtherEnt(ent.Owner);

            if (!_statusEffect.TryGetStatusEffect(other, _effectID, out var otherEffectEnt) || !HasComp<PhysicsComponent>(other)
                || !TryComp<BiomagneticPolarizationStatusEffectComponent>(otherEffectEnt, out var otherBiomagComp))
                continue;

            (oppositePolarityCollision, samePolarityCollision, strengthCapInvolved) = BiomagCollide((ent, biomagComp), (other, otherBiomagComp));
        }
        return (oppositePolarityCollision, samePolarityCollision, strengthCapInvolved);
    }

    protected (bool Opposite, bool Same, bool StrCapInvolved) BiomagCollide(Entity<BiomagneticPolarizationStatusEffectComponent> self, Entity<BiomagneticPolarizationStatusEffectComponent> other)
    {
        var biomagComp = self.Comp;
        var xform = Transform(self);
        var otherBiomagComp = other.Comp;
        var samePolarityCollision = false;
        var oppositePolarityCollision = false;
        var strengthCapInvolved = false;

        // if two people with the same charge come in contact, they repel one another
        if (biomagComp.Polarization == otherBiomagComp.Polarization)
        {
            samePolarityCollision = true;
            var xformQuery = GetEntityQuery<TransformComponent>();
            var worldPos = _xform.GetWorldPosition(xform, xformQuery);

            var otherXform = Transform(other);
            var otherWorldPos = _xform.GetWorldPosition(otherXform, xformQuery);

            var direction = otherWorldPos - worldPos;
            var otherDirection = worldPos - otherWorldPos;

            var strengthAverage = (biomagComp.CurrentStrength + otherBiomagComp.CurrentStrength) / 2;
            // balance the strength of both parties
            biomagComp.CurrentStrength = strengthAverage;
            otherBiomagComp.CurrentStrength = strengthAverage;
            // chuck shit
            _throw.TryThrow(self, direction * -(strengthAverage / 2), strengthAverage * biomagComp.ThrowStrengthMult);
            _throw.TryThrow(other, otherDirection * -(strengthAverage / 2), strengthAverage * otherBiomagComp.ThrowStrengthMult);
        }
        // if two people with DIFFERENT charge come in contact, they cause an explosion and lose their charge.
        else if (biomagComp.Polarization != otherBiomagComp.Polarization)
        {
            var strengthCapWithMargin = biomagComp.StrengthCap - biomagComp.CapEffectMargin;
            if (biomagComp.CurrentStrength > strengthCapWithMargin || otherBiomagComp.CurrentStrength > strengthCapWithMargin)
                strengthCapInvolved = true;
            oppositePolarityCollision = true;
            otherBiomagComp.Expired = true;
        }
        return (oppositePolarityCollision, samePolarityCollision, strengthCapInvolved);
    }
}
