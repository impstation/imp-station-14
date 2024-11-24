using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared._Impstation.Cosmiccult.Components;
using Content.Shared.Stunnable;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Content.Shared.Antag;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Cosmiccult;

public abstract class SharedCosmicCultSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, ComponentGetStateAttemptEvent>(OnCosmicCultCompGetStateAttempt);
        SubscribeLocalEvent<CosmicCultLeadComponent, ComponentGetStateAttemptEvent>(OnCosmicCultCompGetStateAttempt);
        SubscribeLocalEvent<CosmicCultComponent, ComponentStartup>(DirtyCosmicCultComps);
        SubscribeLocalEvent<CosmicCultLeadComponent, ComponentStartup>(DirtyCosmicCultComps);
    }

    /// <summary>
    /// Determines if a Cosmic Cult Lead component should be sent to the client.
    /// </summary>
    private void OnCosmicCultCompGetStateAttempt(EntityUid uid, CosmicCultLeadComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    /// <summary>
    /// Determines if a Cosmic Cultist component should be sent to the client.
    /// </summary>
    private void OnCosmicCultCompGetStateAttempt(EntityUid uid, CosmicCultComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    /// <summary>
    /// The criteria that determine whether a Cult Member component should be sent to a client.
    /// </summary>
    /// <param name="player"> The Player the component will be sent to.</param>
    /// <returns></returns>
    private bool CanGetState(ICommonSession? player)
    {
        //Apparently this can be null in replays so I am just returning true.
        if (player?.AttachedEntity is not {} uid)
            return true;

        if (HasComp<CosmicCultComponent>(uid) || HasComp<CosmicCultLeadComponent>(uid))
            return true;

        return HasComp<ShowAntagIconsComponent>(uid);
    }

    /// <summary>
    /// Dirties all the Cult components so they are sent to clients.
    ///
    /// We need to do this because if a Cult component was not earlier sent to a client and for example the client
    /// becomes a Cult then we need to send all the components to it. To my knowledge there is no way to do this on a
    /// per client basis so we are just dirtying all the components.
    /// </summary>
    private void DirtyCosmicCultComps<T>(EntityUid someUid, T someComp, ComponentStartup ev)
    {
        var cosmicCultComps = AllEntityQuery<CosmicCultComponent>();
        while (cosmicCultComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }

        var cosmicCultLeadComps = AllEntityQuery<CosmicCultLeadComponent>();
        while (cosmicCultLeadComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }
    }
}



///  USER INTERFACE HANDLING GOES HEEEEEEEEEEEERE


[Serializable, NetSerializable]
public enum CosmicMonumentUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CosmicMonumentUserInterfaceState : BoundUserInterfaceState
{
    public TimeSpan CooldownEndTime;

    public int FuelAmount;

    public int FuelCost;

    public CosmicMonumentUserInterfaceState(TimeSpan cooldownEndTime, int fuelAmount, int fuelCost)
    {
        CooldownEndTime = cooldownEndTime;
        FuelAmount = fuelAmount;
        FuelCost = fuelCost;
    }
}

[Serializable, NetSerializable]
public sealed class CosmicMonumentGenerateButtonPressedEvent : BoundUserInterfaceMessage
{

}
