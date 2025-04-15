using Content.Server.Administration.Logs;
using Content.Server.Doors.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared._Impstation.Keyring.Components;
using Content.Shared._Impstation.Keyring.EntitySystems;

namespace Content.Server._Impstation.Keyring
{
    public sealed class KeyringSystem : SharedKeyringSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        [Dependency] private readonly ExamineSystemShared _examine = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<KeyringComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
        }

        private void OnBeforeInteract(Entity<KeyringComponent> entity, ref BeforeRangedInteractEvent args)
        {
            var isAirlock = TryComp<AirlockComponent>(args.Target, out var airlockComp);

            if (args.Handled
                || args.Target == null
                || !TryComp<DoorComponent>(args.Target, out var doorComp) // If it isn't a door we don't use it
                // Only able to control doors if they are within your vision and within your max range.
                // Not affected by mobs or machines anymore.
                || !_examine.InRangeUnOccluded(args.User,
                    args.Target.Value,
                    SharedInteractionSystem.InteractionRange,
                    null))

            {
                return;
            }

            args.Handled = true;

            if (!this.IsPowered(args.Target.Value, EntityManager))
            {
                _popup.PopupEntity(Loc.GetString("door-remote-no-power"), args.User, args.User);
                return;
            }

            if (TryComp<AccessReaderComponent>(args.Target, out var accessComponent)
                && !_doorSystem.HasAccess(args.Target.Value, args.Used, doorComp, accessComponent))
            {
                if (isAirlock)
                    _doorSystem.Deny(args.Target.Value, doorComp, args.User);
                _popup.PopupEntity(Loc.GetString("door-remote-denied"), args.User, args.User);
                return;
            }

            if (_doorSystem.TryToggleDoor(args.Target.Value, doorComp, args.Used))
            {
                _adminLogger.Add(LogType.Action,
                    LogImpact.Medium,
                    $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)}: {doorComp.State}");
            }
        }
    }
}
