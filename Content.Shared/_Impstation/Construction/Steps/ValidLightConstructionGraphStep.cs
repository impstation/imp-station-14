using Content.Shared.Construction.Steps;
using Content.Shared.Light.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Construction.Steps
{
    /// <summary>
    /// Scuffed copy of <see cref="MultipleTagsConstructionGraphStep"/>, but with validation that the inserted lightbulb isn't broken.
    /// </summary>
    public sealed partial class ValidLightConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("lightAllTags")]
        private List<ProtoId<TagPrototype>>? _lightAllTags;

        [DataField("lightAnyTags")]
        private List<ProtoId<TagPrototype>>? _lightAnyTags;

        private static bool IsNullOrEmpty<T>(ICollection<T>? list)
        {
            return list == null || list.Count == 0;
        }

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            // This step can only happen if either list has tags.
            if (IsNullOrEmpty(_lightAllTags) && IsNullOrEmpty(_lightAnyTags))
                return false; // Step is somehow invalid, we return.

            entityManager.TryGetComponent<LightBulbComponent>(uid, out var bulb);
            if (bulb == null || bulb.State != LightBulbState.Normal)
                return false; // Not a bulb, or the bulb is broken or burned

            var tagSystem = entityManager.EntitySysManager.GetEntitySystem<TagSystem>();

            if (_lightAllTags != null && !tagSystem.HasAllTags(uid, _lightAllTags))
                return false; // We don't have all the tags needed.

            if (_lightAnyTags != null && !tagSystem.HasAnyTag(uid, _lightAnyTags))
                return false; // We don't have any of the tags needed.

            // This entity is valid!
            return true;
        }
    }
}
