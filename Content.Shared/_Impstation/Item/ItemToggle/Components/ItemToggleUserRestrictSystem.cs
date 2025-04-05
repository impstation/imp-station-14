using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using System.Collections.Immutable;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Item.ItemToggle.Components
{
    public sealed class ItemToggleUserRestrictSystem : EntitySystem
    {

        [Dependency] private readonly IPrototypeManager _prot = default!;
        [Dependency] private readonly IEntityManager _ent = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        private ISawmill _sawmill = default!;



        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ItemToggleUserRestrictComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
            SubscribeLocalEvent<ItemToggleUserRestrictComponent, ItemToggleDeactivateAttemptEvent>(OnDeactivateAttempt);

        }

        private void OnActivateAttempt(Entity<ItemToggleUserRestrictComponent> ent, ref ItemToggleActivateAttemptEvent args)
        {
            if (ent.Comp.OpenRestrict)
            {
                //there has to be an easier way to do this but i can't find it!
                var requiredComps = ent.Comp.Components.Keys.ToImmutableList();
                foreach (var requiredComp in requiredComps)
                {
                    var registration = _componentFactory.GetRegistration(requiredComp);
                    if (registration != null && args.User != null)
                    {
                        if (!_ent.HasComponent(args.User.Value, registration))
                        {
                            args.Cancelled = true;
                            args.Popup = Loc.GetString(ent.Comp.RestrictMessage);
                            break;
                        }
                    }
                }
            }
        }

        private void OnDeactivateAttempt(Entity<ItemToggleUserRestrictComponent> ent, ref ItemToggleDeactivateAttemptEvent args)
        {
            if (ent.Comp.CloseRestrict)
            {
                var requiredComps = ent.Comp.Components.Keys.ToImmutableList();
                foreach (var requiredComp in requiredComps)
                {
                    var registration = _componentFactory.GetRegistration(requiredComp);
                    if (registration != null && args.User != null)
                    {
                        if (!_ent.HasComponent(args.User.Value, registration))
                        {
                            args.Cancelled = true;
                            //idk why itemtoggledeactivatedattemptevent doesn't have a popup and i'm tired of trying to give it one
                            break;
                        }
                    }
                }
            }
        }
    }
}
