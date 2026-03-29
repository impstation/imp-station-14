using Content.Client._ES.Lighting;
using Content.Client._ES.Lighting.Components;
using Content.Shared.Audio;
using Robust.Shared.Audio;

namespace Content.Client._ES.Audio.Ambience;

/// <summary>
///     Used to control properties of <see cref="AmbientSoundComponent"/> via appearance data on <see cref="AppearanceComponent"/>.
///     Controlled ambient sounds should be netsync false.
/// </summary>
/// <remarks>
///     This is kind of weird, but it makes sense to me
/// </remarks>
[RegisterComponent]
public sealed partial class ESGenericAmbienceVisualizerComponent : Component
{
    /// <summary>
    /// Nested dictionary that maps appearance data keys -> data value -> ambient sound data
    /// </summary>
    [DataField(required:true)]
    public Dictionary<Enum, Dictionary<string, ESAmbienceData>> Sounds = new();
}

[DataRecord]
public partial record ESAmbienceData(SoundSpecifier? Sound, float? Range, float? Volume, bool Enabled = true);
