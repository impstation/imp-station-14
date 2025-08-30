/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Server.Temperature.Components;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body.Events;
using Content.Shared.Medical.Cryogenics;

namespace Content.Server._Offbrand.Wounds;

public sealed class CryostasisFactorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InsideCryoPodComponent, GetMetabolicMultiplierEvent>(OnGetMetabolicMultiplier);
    }

    private void OnGetMetabolicMultiplier(Entity<InsideCryoPodComponent> ent, ref GetMetabolicMultiplierEvent args)
    {
        if (!TryComp<CryostasisFactorComponent>(ent, out var stasis))
            return;

        if (!TryComp<TemperatureComponent>(ent, out var temp))
            return;

        args.Multiplier *= Math.Max(stasis.TemperatureCoefficient * temp.CurrentTemperature + stasis.TemperatureConstant, 1);
    }
}
