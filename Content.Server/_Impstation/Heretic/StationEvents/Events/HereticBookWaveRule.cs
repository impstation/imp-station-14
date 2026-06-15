using Content.Server._Impstation.Heretic.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Server._Impstation.Heretic.StationEvents.Events;

public sealed class HereticBookWaveRule : StationEventSystem<HereticBookWaveRuleComponent>
{
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    protected override void Started(EntityUid uid, HereticBookWaveRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        for (var i = 0; i < _rand.Next(component.MinBooks, component.MaxBooks); i++)
            if (TryFindRandomTile(out var _, out var _, out var _, out var coords))
            {
                _audio.PlayPvs(component.RiftSpawnSound, Spawn(component.RealityTearPrototype, coords),
                AudioParams.Default.WithMaxDistance(15f).WithRolloffFactor(0.8f));
                //reality tears disappear after 10 seconds, leaving behind an eldritch book
            }
    }
}
