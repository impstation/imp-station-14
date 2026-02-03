using Content.Shared.GameTicking;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.CollectiveMind;

public sealed class CollectiveMindUpdateSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly Dictionary<CollectiveMindPrototype, int> GlobalMindIdTracker = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CollectiveMindComponent, ComponentStartup>(OnCollectiveMindInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnCollectiveMindInit(EntityUid uid, CollectiveMindComponent component, ComponentStartup args)
    {
        UpdateCollectiveMind(uid, component);
    }

    private static void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        GlobalMindIdTracker.Clear();
    }

    public void ForceCloneFrom(EntityUid sourceuid, EntityUid targetuid)
    {
        if (!TryComp<CollectiveMindComponent>(sourceuid, out var component))
            return;

        if (!TryComp<CollectiveMindComponent>(targetuid, out var targetComponent))
            return;

        targetComponent.Minds.Clear();

        foreach (var mind in component.Minds)
        {
            targetComponent.Minds.Add(mind.Key, mind.Value);
        }

        UpdateCollectiveMind(targetuid, targetComponent); //capture any we missed
    }

    public void UpdateCollectiveMind(EntityUid uid, CollectiveMindComponent collective)

    {
        foreach (var prototype in _prototypeManager.EnumeratePrototypes<CollectiveMindPrototype>())
        {
            //check if they dont already have it
            if (collective.Minds.ContainsKey(prototype))
                continue;

            var components = StringsToRegs(prototype.RequiredComponents);

            var meetsRequirements = false;

            foreach (var component in components)
            {
                if (HasComp(uid, component.Type))
                {
                    meetsRequirements = true;
                    break;
                }
            }

            foreach (var tag in prototype.RequiredTags)
            {
                if (_tag.HasTag(uid, tag))
                {
                    meetsRequirements = true;
                    break;
                }
            }

            if (meetsRequirements)
            {
                collective.Minds.TryAdd(prototype, CreateNewCollectiveMindMemberData(prototype));
            }
            else
            {
                collective.Minds.Remove(prototype);
            }
        }
    }

    private List<ComponentRegistration> StringsToRegs(List<string> input)
    {
        var list = new List<ComponentRegistration>();

        foreach (var name in input)
        {
            if (!_componentFactory.TryGetRegistration(name, out var registration))
            {
                Log.Error(
                    $"StringsToRegs failed: Unknown component name {name} passed to {nameof(CollectiveMindUpdateSystem)}.");
                continue;
            }

            list.Add(registration);
        }

        return list;
    }

    private static CollectiveMindMemberData CreateNewCollectiveMindMemberData(CollectiveMindPrototype prototype)
    {
        // Initialize the tracker value for this prototype.
        GlobalMindIdTracker.TryAdd(prototype, CollectiveMindMemberData.StartingId);

        return new CollectiveMindMemberData { MindId = GlobalMindIdTracker[prototype]++ };
    }
}
