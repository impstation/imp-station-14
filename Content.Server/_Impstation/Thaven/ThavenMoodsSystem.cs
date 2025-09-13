using Content.Server._Impstation.StrangeMoods;
using Content.Server.StationEvents.Events;
using Content.Shared._Impstation.StrangeMoods;
using Content.Shared._Impstation.Thaven;
using Content.Shared.Emag.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Impstation.Thaven;

public sealed class ThavenMoodsSystem : SharedThavenMoodsSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StrangeMoodsSystem _moods = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThavenMoodsComponent, IonStormEvent>(OnIonStorm);
    }

    private void OnIonStorm(Entity<ThavenMoodsComponent> ent, ref IonStormEvent args)
    {
        if (!ent.Comp.IonStormable || !TryComp<StrangeMoodsComponent>(ent, out var moods))
            return;

        // remove mood
        if (_random.Prob(ent.Comp.IonStormRemoveChance) && moods.Moods.Count > 1)
        {
            var removeIndex = _random.Next(moods.Moods.Count);
            moods.Moods.RemoveAt(removeIndex);
            Dirty(ent);

            _moods.NotifyMoodChange((ent, moods));
        }

        // add mood
        else if (_random.Prob(ent.Comp.IonStormAddChance) && moods.Moods.Count <= ent.Comp.MaxIonMoods)
            _moods.TryAddRandomMood((ent, moods), ent.Comp.IonStormDataset);

        // replace mood
        else
        {
            var conflicts = new HashSet<ProtoId<StrangeMoodPrototype>>();

            if (moods.Moods.Count > 1)
            {
                var removeIndex = _random.Next(moods.Moods.Count);

                if (moods.Moods[removeIndex].ProtoId is { } moodProto)
                    conflicts.Add(moodProto);

                moods.Moods.RemoveAt(removeIndex);
            }

            _moods.TryAddRandomMood((ent, moods), ent.Comp.IonStormDataset, conflicts: conflicts);
        }
    }

    protected override void OnEmagged(Entity<ThavenMoodsComponent> ent, ref GotEmaggedEvent args)
    {
        base.OnEmagged(ent, ref args);

        if (!args.Handled || !TryComp<StrangeMoodsComponent>(ent, out var moods))
            return;

        _moods.TryAddRandomMood((ent, moods), ent.Comp.WildcardDataset);
    }
}
