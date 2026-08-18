using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Heretic.Ritual

{
    [ImplicitDataDefinitionForInheritors]
    public partial interface IRitualBehavior
    {
        /// <summary>
        /// Check the ritual conditions.
        /// </summary>
        /// <param name="performer">Entity doing the ritual.</param>
        /// <param name="platform">The platform that the ritual is being done on.</param>
        /// <param name="ritualId">The ID of the ritual.</param>
        /// <returns> If the conditions succeeded or not.</returns>
        bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId);

        /// <summary>
        /// Perform the ritual effect
        /// </summary>
        /// <param name="performer">Entity doing the ritual.</param>
        /// <param name="platform">The platform that the ritual is being done on.</param>
        /// <param name="ritualId">The ID of the ritual.</param>
        void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId);
    }
}
