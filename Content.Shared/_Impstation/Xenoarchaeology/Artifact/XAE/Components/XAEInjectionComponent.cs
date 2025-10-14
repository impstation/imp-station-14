using Content.Shared._Impstation.Xenoarchaeology.Artifact.XAE.Systems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Xenoarchaeology.Artifact.XAE.Components;

[RegisterComponent, Access(typeof(XAEInjectionSystem)), NetworkedComponent]
public sealed partial class XAEInjectionComponent : Component
{
    [DataDefinition]
    public sealed partial class ChemEntry
    {
        [DataField("chemical"), ViewVariables(VVAccess.ReadWrite)]
        public string Chemical = "Water";

        [DataField("amount"), ViewVariables(VVAccess.ReadWrite)]
        public float Amount = 1f;
    }

    [DataField]
    public ChemEntry[] Entries { get; private set; } = Array.Empty<ChemEntry>();

    [DataField]
    public float Range = 5f;

    [DataField]
    public Solution ChemicalSolution = default!;

    [DataField]
    public EntProtoId VisualEffectPrototype = "PuddleSparkle";

    [DataField]
    public bool ShowEffect = true;
}
