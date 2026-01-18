using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared.EntityTable;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Power;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.AnimalHusbandry.EntitySystems;
public sealed class IncubationSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityTableSystem _entTable = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<IncubatorComponent>();
        while(query.MoveNext(out var uid, out var incuComp))
        {

        }
    }
}
