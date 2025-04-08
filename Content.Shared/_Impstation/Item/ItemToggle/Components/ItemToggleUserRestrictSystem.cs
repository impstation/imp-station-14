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
            if (!ent.Comp.OpenRestrict || args.Cancelled)
            {
                return;
            }

            foreach (var reg in ent.Comp.Components.Values)
            {
                var type = reg.Component.GetType();
                if (!HasComp(args.User, type))
                {
                    args.Cancelled = true;
                    if(ent.Comp.RestrictMessage != null)
                    {
                        args.Popup = Loc.GetString(ent.Comp.RestrictMessage);
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private void OnDeactivateAttempt(Entity<ItemToggleUserRestrictComponent> ent, ref ItemToggleDeactivateAttemptEvent args)
        {
            if (!ent.Comp.CloseRestrict || args.Cancelled)
            {
                return;
            }

            foreach (var reg in ent.Comp.Components.Values)
            {
                var type = reg.Component.GetType();
                if (!HasComp(args.User, type))
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
