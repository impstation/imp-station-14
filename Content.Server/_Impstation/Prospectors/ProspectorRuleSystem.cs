using Content.Server.Antag;
using Content.Server.Mind;
using Content.Server.GameTicking.Rules;
using Content.Server._Impstation.Prospectors.Components;
using Content.Server.Roles;
using Content.Shared._Impstation.Prospectors.Components;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Content.Server.Radio.Components;
using Robust.Shared.Player;
using Content.Server.EUI;
using Robust.Shared.Random;
using Content.Server.Announcements.Systems;
using Robust.Server.Audio;
using Content.Shared.Coordinates;
using Content.Shared.Parallax;
using Robust.Shared.Map.Components;
using Content.Shared.Temperature.Components;
using Content.Server.Atmos.Components;
using Content.Server.Objectives.Components;
using Robust.Server.Player;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Events;
using Content.Shared.Stunnable;
using Content.Shared.Mind;
using Content.Server.Actions;
using Robust.Server.GameObjects;
using Content.Server.Station.Systems;
using Robust.Shared.Timing;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Server.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using System.Linq;
// using Content.Server._Impstation.Prospectors.EntitySystems;
using Content.Server.Shuttles.Systems;
// using Content.Shared._Impstation.Prospectors.Components.Examine;
using Content.Shared.Mind.Components;
using Content.Shared.Body.Systems;
using Content.Server.RoundEnd;
using Content.Server.Audio;
using Content.Shared.Audio;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage;
using Content.Server.Bible.Components;
using Content.Shared.UserInterface;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server._Impstation.Prospectors;
// using Content.Shared._Impstation.Prospectors.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using Content.Shared.Mobs.Systems;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Shared.IdentityManagement;
using Content.Shared._Impstation.Prospectors;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Server.CrewManifest;
using Content.Shared.Cuffs.Components;

namespace Content.Server._Impstation.Prospectors;

/// <summary>
/// Where all the main stuff for Prospectors happens (Checking Prospector living/uncuffed status, ending round, etc )
/// </summary>
public sealed class ProspectorRuleSystem : GameRuleSystem<ProspectorRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProspectorComponent, MobStateChangedEvent>(OnMobStateChanged);

    }

    protected override void Started(EntityUid uid, ProspectorRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        component.ProspCheck = _timing.CurTime + component.TimerWait;
    }

    protected override void ActiveTick(EntityUid uid, ProspectorRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
        if (component.ProspCheck <= _timing.CurTime)
        {
            component.ProspCheck = _timing.CurTime + component.TimerWait;

            if (CheckProspLose())
            {
                _roundEnd.DoRoundEndBehavior(RoundEndBehavior.ShuttleCall, component.EvacShuttleTime);
                GameTicker.EndGameRule(uid, gameRule);
            }
        }
    }

    protected override void AppendRoundEndText(EntityUid uid,
        ProspectorRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var prospLost = CheckProspLose();
        var index = (prospLost ? 2 : 0);
        args.AddLine(Loc.GetString(Outcomes[index]));

        var sessionData = _antag.GetAntagIdentifiers(uid);
        args.AddLine(Loc.GetString("prospector-count", ("initialCount", sessionData.Count)));
        foreach (var (mind, data, name) in sessionData)
        {
            _role.MindHasRole<ProspectorRoleComponent>(mind, out var role);

            args.AddLine(Loc.GetString("prospector-name-user",
                ("name", name),
                ("username", data.UserName)));

        }
    }

    //Runs Prospector loss state check if a Prospector is arrested or killed.
    private void OnMobStateChanged(EntityUid uid, ProspectorComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckProspLose();
    }

    /// <summary>
    /// Checks if all of the Prospectors are dead or arrested and will report it for game end.
    /// </summary>
    private bool CheckProspLose()
    {
        var prospList = new List<EntityUid>();

        var prospectors = AllEntityQuery<ProspectorComponent>();
        while (prospectors.MoveNext(out var id, out _))
        {
            prospList.Add(id);
        }

        return IsGroupDetainedOrDead(prospList, true, true);
    }

    private void OnProspectorStateChanged(EntityUid uid, ProspectorComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckProspLose();
    }

    /// <summary>
    /// Will take a group of entities and check if these entities are alive, dead or cuffed. Lifted from the Revolutionaries Rule system.
    /// </summary>
    /// <param name="list">The list of the entities</param>
    /// <param name="checkOffStation">Bool for if you want to check if someone is in space and consider them missing in action. (Won't check when emergency shuttle arrives just in case)</param>
    /// <param name="countCuffed">Bool for if you don't want to count cuffed entities.</param>
    /// <returns></returns>
    private bool IsGroupDetainedOrDead(List<EntityUid> list, bool checkOffStation, bool countCuffed)
    {
        var gone = 0;
        foreach (var entity in list)
        {
            if (TryComp<CuffableComponent>(entity, out var cuffed) && cuffed.CuffedHandCount > 0 && countCuffed)
            {
                gone++;
            }
            else
            {
                if (TryComp<MobStateComponent>(entity, out var state))
                {
                    if (state.CurrentState == MobState.Dead || state.CurrentState == MobState.Invalid)
                    {
                        gone++;
                    }
                    else if (checkOffStation && _stationSystem.GetOwningStation(entity) == null && !_emergencyShuttle.EmergencyShuttleArrived)
                    {
                        gone++;
                    }
                }
                //If they don't have the MobStateComponent they might as well be dead.
                else
                {
                    gone++;
                }
            }
        }

        return gone == list.Count || list.Count == 0;
    }

    private static readonly string[] Outcomes =
    {
        // revs won and heads died
        "prospector-major",
        // revs lost and heads survived
        "prospector-minor",
        // revs lost and heads died
        "neutral",
        "crew-minor",
        "crew-major"
    };
}
