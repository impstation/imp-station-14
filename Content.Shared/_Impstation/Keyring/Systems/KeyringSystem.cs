using Content.Shared._Impstation.Keyring.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.Prying.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Impstation.Keyring.Systems;

/// <summary>
/// Handles prying of entities (e.g. doors) using keyrings
/// </summary>
public sealed class KeyringSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly PryingSystem _pry = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KeyringComponent, AfterInteractEvent>(TryKeyDoor);
    }

    private void TryKeyDoor(EntityUid uid, KeyringComponent comp, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        //if(!args.target.accesses has a match in comp.AccessList)
            //looc popup "invalid access"
            //maybe make the door beep... could probably hack this just by simulating an attempt to open event?
            //return;

        args.Handled = TryKey(args.Target, uid, out _, args.Used); //i hate nullables!!!!!!!
    }

    private void OnDoorAltVerb(EntityUid uid, DoorComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<PryingComponent>(args.User, out _))
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("door-pry"),
            Impact = LogImpact.Low,
            Act = () => TryKey(args.Target, uid, out _, args.User), //gotta be real i got no clue whats goin on here
        });
    }

    /// <summary>
    /// Try to key a door.
    /// </summary>
    public bool TryKey(EntityUid target, EntityUid user, out DoAfterId? id, EntityUid tool)
    {
        id = null;

        EnsureComp<KeyringComponent>(tool, out var comp);
        //idk what all the stuff in trypry does but it seems to just check if a door is being pried open by hand, so we'll skip it. fully prepared to be wrong on that tho

        return _pry.StartPry(target, user, null, comp.OpenTime, out id);
        //"is inaccessible due to its protection level" dog wtf!!!!!!!!!!! help
    }
}
