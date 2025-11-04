using Content.Shared._Impstation.PersonalEconomy.Components;
using Content.Shared.Administration;
using Content.Shared.Examine;
using Content.Shared.Roles;

namespace Content.Shared._Impstation.PersonalEconomy.Systems;

/// <summary>
/// The main banking system; handles all funds transfers and keeps track of bank accounts etc etc
/// </summary>
public abstract class SharedBankingSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = null!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BankCardComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BankCardComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<BankCardComponent> ent, ref ComponentInit args)
    {
        SetupID(ent);
    }

    protected virtual void SetupID(Entity<BankCardComponent> ent)
    {

    }

    private void OnExamined(Entity<BankCardComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("bank-card-examine-access-number", ("number", $"{ent.Comp.AccessNumber:000000}")),4);
        args.PushMarkup(Loc.GetString("bank-card-examine-transfer-number", ("number", $"{ent.Comp.TransferNumber:0000}")),4);

        args.PushMarkup("The two below are for testing!", 3);
        args.PushMarkup(Loc.GetString("bank-card-examine-balance", ("balance", GetAccount(ent.Comp.AccessNumber)!.Balance)), 2); //todo remove this
        args.PushMarkup(Loc.GetString("bank-card-examine-salary", ("salary", GetAccount(ent.Comp.AccessNumber)!.Salary)), 1); //todo remove this
    }

    /// <summary>
    /// Create a new account. does nothing if run on the client.
    /// </summary>
    /// <returns>the Access number &amp; transfer number for the account</returns>
    public virtual (int AccessNumber, int TransferNumber) CreateNewAccount(string name)
    {
        return (0, 0);
    }

    private BankAccount? GetAccount(int accessNumber)
    {
        var serverQuery = EntityQueryEnumerator<RemoteBankServerComponent>();
        return serverQuery.MoveNext(out var uid, out var server) ? server.AccountDict.GetValueOrDefault(accessNumber) : null;
    }

    /// <summary>
    /// Set the name for an account
    /// </summary>
    /// <param name="accessNumber"></param>
    /// <param name="name"></param>
    public virtual void SetAccountName(int accessNumber, string name)
    {
        var account = GetAccount(accessNumber);

        if (account != null)
            account.Name = name;
    }

    public virtual void SetAccountSalary(int accessNumber, int salary)
    {
        var account = GetAccount(accessNumber);

        if (account != null)
            account.Salary = salary;
    }

    public virtual void SetAccountBalance(int accessNumber, int balance)
    {
        var account = GetAccount(accessNumber);

        if (account != null)
            account.Balance = balance;
    }

    /// <summary>
    /// Update the details on a bank card to reflect the details of a given account.
    /// </summary>
    /// <param name="card"></param>
    /// <param name="accessNumber"></param>
    public virtual void UpdateCardDetails(Entity<BankCardComponent> card, int accessNumber)
    {
        var account = GetAccount(accessNumber);

        if (account is null)
            return;

        SetCardName(card, account.Name);
        SetCardNumber(card, account.AccessNumber);
    }

    /// <summary>
    /// Set the name on a card
    /// </summary>
    /// <param name="card"></param>
    /// <param name="name"></param>
    public virtual void SetCardName(Entity<BankCardComponent> card, string name)
    {
        card.Comp.Name = name;
        _metaData.SetEntityName(card, Loc.GetString(card.Comp.NamedLocId, ("name", name)));
        Dirty(card);
    }

    /// <summary>
    /// set the number on a card
    /// </summary>
    /// <param name="card"></param>
    /// <param name="accessNumber"></param>
    public virtual void SetCardNumber(Entity<BankCardComponent> card, int accessNumber)
    {
        card.Comp.AccessNumber = accessNumber;
        Dirty(card);
    }
}
