using Content.Server.GameTicking.Events;
using Content.Shared._Impstation.PersonalEconomy.Components;
using Content.Shared._Impstation.PersonalEconomy.Systems;
using Content.Shared.GameTicking;
using Robust.Server.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Impstation.PersonalEconomy;

/// <summary>
/// This handles...
/// </summary>
public sealed class ServerBankingSystem : SharedBankingSystem
{

    [Dependency] private SharedMapSystem _map = null!;
    [Dependency] private MetaDataSystem _metaData = null!;
    [Dependency] private SharedTransformSystem _xform = null!;
    [Dependency] private PvsOverrideSystem _pvsOverride = null!;
    [Dependency] private IRobustRandom _random = null!;

    private EntityUid _cheeseWorld;
    private readonly EntProtoId _remoteServerProto = "RemoteBankServer";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        _cheeseWorld = EnsurePausedMap();
        var server = Spawn(_remoteServerProto);
        _xform.SetParent(server, _cheeseWorld);
        _pvsOverride.AddForceSend(server); //probably not *great*, but every client needs to know about the banking server
    }

    /// <summary>
    /// cleans up cheese world
    /// </summary>
    /// <param name="ev"></param>
    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        if (!Exists(_cheeseWorld))
            return;

        Del(_cheeseWorld);
    }

    /// <summary>
    /// creates a map for the banking server to live in
    /// </summary>
    private EntityUid EnsurePausedMap()
    {
        if (Exists(_cheeseWorld))
            return _cheeseWorld;

        var mapUid = _map.CreateMap();
        _metaData.SetEntityName(mapUid, "cheese world. (for banking)");
        return mapUid;
    }

    protected override void SetupID(Entity<BankCardComponent> ent)
    {
        var details = CreateNewAccount("Unknown");
        ent.Comp.AccessNumber = details.AccessNumber;
        ent.Comp.TransferNumber = details.TransferNumber;
        SetAccountSalary(details.AccessNumber, ent.Comp.Salary);
        SetAccountBalance(details.AccessNumber, ent.Comp.StartingBalance);
        Dirty(ent);
    }

    /// <inheritdoc/>
    public override (int AccessNumber, int TransferNumber) CreateNewAccount(string name)
    {
        var serverQuery = EntityQueryEnumerator<RemoteBankServerComponent>();
        if (serverQuery.MoveNext(out var uid, out var server))
        {
            //generate a unique ID
            var accountNo = _random.Next(0, 1000000);
            while (server.AccountDict.ContainsKey(accountNo))
            {
                accountNo = _random.Next(0, 1000000);
            }

            //generate a unique transfer number
            var transferNo = _random.Next(0, 10000);
            while (server.TransferNumberToAccountNumberDict.ContainsKey(accountNo))
            {
                transferNo = _random.Next(0, 10000);
            }

            //create a new account, put it in the accounts dict
            server.AccountDict[accountNo] = new BankAccount
            {
                AccessNumber = accountNo,
                TransferNumber = transferNo,
                Name = name,
            };

            //and map the transfer number to the account
            server.TransferNumberToAccountNumberDict[transferNo] = accountNo;

            Dirty<RemoteBankServerComponent>((uid, server));
            return (accountNo, transferNo);
        }

        //todo error handling for if the remote server somehow gets deleted
        return (0, 0);
    }
}
