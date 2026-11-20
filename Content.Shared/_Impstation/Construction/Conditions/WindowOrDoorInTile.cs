using Content.Shared.Construction;
using Content.Shared.Construction.Conditions;
using Content.Shared.Doors.Components;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class WindowOrDoorInTile : IConstructionCondition
    {
        private static readonly ProtoId<TagPrototype> WindowTag = "Window";
        public bool Condition(EntityUid _, EntityCoordinates location, Direction direction)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            var sysMan = entManager.EntitySysManager;
            var tagSystem = sysMan.GetEntitySystem<TagSystem>();
            var lookupSys = sysMan.GetEntitySystem<EntityLookupSystem>();

            foreach (var entity in lookupSys.GetEntitiesIntersecting(location, LookupFlags.Static))
            {
                if (tagSystem.HasTag(entity, WindowTag))
                    return true;

                if (entManager.HasComponent<DoorComponent>(entity))
                    return true;
            }

            return false;
        }

        public ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry
            {
                Localization = "construction-step-condition-window-or-door-in-tile"
            };
        }
    }
}
