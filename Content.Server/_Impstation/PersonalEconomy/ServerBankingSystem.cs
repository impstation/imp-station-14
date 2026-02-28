using System.Diagnostics;
using Content.Server.GameTicking.Events;
using Content.Shared._Impstation.PersonalEconomy.Components;
using Content.Shared._Impstation.PersonalEconomy.Events;
using Content.Shared._Impstation.PersonalEconomy.Systems;
using Content.Shared.GameTicking;
using Robust.Server.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

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
    private readonly EntProtoId _bankAccountProto = "BankAccount";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<BankCardComponent, ComponentInit>(OnComponentInit);
    }

    //todo should this be a different event?
    private void OnComponentInit(Entity<BankCardComponent> ent, ref ComponentInit args)
    {
        SetupID(ent);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        _cheeseWorld = EnsurePausedMap();
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

    private void SetupID(Entity<BankCardComponent> ent)
    {
        var account = CreateNewAccount("Unknown");
        ent.Comp.AccessNumber = account.Comp.AccessNumber;
        ent.Comp.TransferNumber = account.Comp.TransferNumber;
        SetAccountSalary(account.Comp.AccessNumber, ent.Comp.Salary);
        SetAccountBalance(account.Comp.AccessNumber, ent.Comp.StartingBalance);
        Dirty(ent);
    }

    private Entity<BankAccountComponent> CreateNewAccount(string name)
    {

        //since we don't cache the list of accounts anywhere, first we need to build a list of every transfer number & every access number
        //todo cache list of accounts
        var accountNumbers = new HashSet<int>();
        var transferNumbers = new HashSet<int>();

        var accountQuery = EntityQueryEnumerator<BankAccountComponent>();
        while (accountQuery.MoveNext(out var uid, out var comp))
        {
            DebugTools.Assert(!accountNumbers.Contains(comp.AccessNumber), "Duplicate account numbers should not exist");
            DebugTools.Assert(!transferNumbers.Contains(comp.TransferNumber), "Duplicate transfer numbers should not exist");

            accountNumbers.Add(comp.AccessNumber);
            transferNumbers.Add(comp.TransferNumber);
        }

        //generate a unique ID
        var accountNo = _random.Next(1, 1000000);
        while (accountNumbers.Contains(accountNo))
        {
            accountNo = _random.Next(1, 1000000);
        }

        //generate a unique transfer number
        var transferNo = _random.Next(1, 10000);
        while (transferNumbers.Contains(accountNo))
        {
            transferNo = _random.Next(1, 10000);
        }

        var newAccount = Spawn(_bankAccountProto);
        _xform.SetParent(newAccount, _cheeseWorld);
        //probably not *great*, but every client needs to know about every bank account at all times because of the way this whole system is set up
        //bank accounts are relatively small (3 comps - xform, meta, bankacc) entities so it's probably fine?
        //there'll also be like, maybe a hundred in a round max? if traitors are Doing Some Shit?
        _pvsOverride.AddForceSend(newAccount);

        //create new account
        var bankComp = Comp<BankAccountComponent>(newAccount);

        bankComp.AccessNumber = accountNo;
        bankComp.TransferNumber = transferNo;
        bankComp.Name = name;

        //and send the comp back off to the client
        Dirty<BankAccountComponent>((newAccount, bankComp));
        return (newAccount, bankComp);
    }
}
