using Content.Server._Impstation.Speech.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server._Impstation.Metadata;

/// <summary>
/// Applies the grayspeak accent to an entity's name and/or description.
/// </summary>
public sealed class GrayspeakMetadataSystem : EntitySystem
{
    [Dependency] private readonly GrayAccentComponentAccentSystem _grayspeak = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrayspeakMetadataComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, GrayspeakMetadataComponent component, ref ComponentStartup args)
    {
        if (!_random.Prob(component.Chance))
            return;

        var metadata = MetaData(uid);

        if (component.ChangeName)
            _metaData.SetEntityName(uid, _grayspeak.Grayspeakify(metadata.EntityName));

        if (component.ChangeDescription)
            _metaData.SetEntityDescription(uid, _grayspeak.Grayspeakify(metadata.EntityDescription));
    }
}
