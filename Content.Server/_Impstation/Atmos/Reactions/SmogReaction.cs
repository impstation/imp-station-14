using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class SmogReaction : IGasReactionEffect
    {

        [DataField("gas")] public int GasId { get; private set; } = 0;

        [DataField("molesMin")] public float MolesMin { get; private set; } = 5;

        [DataField("smokeProto")] public EntProtoId? SmokeProto { get; private set; } = "SmogSmoke";

        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
        {

            // If we're not reacting on a tile, do nothing.
            if (holder is not TileAtmosphere tile)
                return ReactionResult.NoReaction;

            // If we don't have enough moles of the specified gas, do nothing.
            if (mixture.GetMoles(GasId) < MolesMin)
                return ReactionResult.NoReaction;

            atmosphereSystem.TrySpawnAtTile(SmokeProto,tile);

            return ReactionResult.Reacting;
        }

    }
}
