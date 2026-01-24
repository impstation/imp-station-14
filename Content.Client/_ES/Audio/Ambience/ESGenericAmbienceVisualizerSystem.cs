using Content.Shared.Audio;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._ES.Audio.Ambience;

/// <see cref="ESGenericAmbienceVisualizerComponent"/>
public sealed class ESGenericAmbienceVisualizerSystem : VisualizerSystem<ESGenericAmbienceVisualizerComponent>
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambience = default!;

    protected override void OnAppearanceChange(EntityUid uid, ESGenericAmbienceVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<AmbientSoundComponent>(uid, out var ambientSound))
        {
            throw new Exception($"Entity {ToPrettyString(uid)} with {nameof(ESGenericAmbienceVisualizerComponent)} does not have {nameof(AmbientSoundComponent)}!");
        }

        DebugTools.Assert(!ambientSound.NetSyncEnabled, $"Entity {ToPrettyString(uid)} uses appearance-controlled ambience with a netsync'd AmbientSoundComponent! (Did you forget to set netsync: false?)");

        foreach (var (appearanceKey, soundDataDict) in component.Sounds)
        {
            if (!AppearanceSystem.TryGetData(uid, appearanceKey, out var obj, args.Component))
                continue;

            var appearanceValue = obj.ToString();
            if (string.IsNullOrEmpty(appearanceValue))
                continue;

            if (!soundDataDict.TryGetValue(appearanceValue, out var soundData))
                continue;

            _ambience.SetAmbience(uid, soundData.Enabled, ambientSound);
            if (soundData.Sound != null)
                _ambience.SetSound(uid, soundData.Sound, ambientSound);
            if (soundData.Volume != null)
                _ambience.SetVolume(uid, soundData.Volume.Value, ambientSound);
            if (soundData.Range != null)
                _ambience.SetRange(uid, soundData.Range.Value, ambientSound);
        }
    }
}
