/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Wounds;

public sealed class MaximumDamageSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaximumDamageComponent, BeforeDamageCommitEvent>(OnBeforeDamageCommit, before: [typeof(WoundableSystem)]);
    }

    private void OnBeforeDamageCommit(Entity<MaximumDamageComponent> ent, ref BeforeDamageCommitEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var damageable = Comp<DamageableComponent>(ent);

        var dict = damageable.Damage.DamageDict;

        var hasCloned = false;
        foreach (var (type, value) in args.Damage.DamageDict)
        {
            if (!dict.TryGetValue(type, out var currentValue))
                continue;

            if (!ent.Comp.Damage.TryGetValue(type, out var maxValue))
                continue;

            FixedPoint2 delta;
            if (currentValue >= maxValue.Base && maxValue.Factor != FixedPoint2.Zero)
            {
                var factor = maxValue.Factor.Double();
                var @base = maxValue.Base.Double();
                Func<FixedPoint2, double> fn = x => Math.Log( Math.Abs(factor - @base + x.Double()) ) * factor;

                var maximumFromNow = FixedPoint2.New(fn(value + currentValue) - fn(currentValue));

                delta = (value - maximumFromNow);
            }
            else
            {
                delta = (value + currentValue) - maxValue.Base;
            }

            if (delta <= 0)
                continue;

            if (!hasCloned)
            {
                hasCloned = true;
                args.Damage = new(args.Damage);
            }

            args.Damage.DamageDict[type] -= delta;
        }

        args.Damage.TrimZeros();
    }
}
