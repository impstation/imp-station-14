using System.Diagnostics.CodeAnalysis;
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

        if (!TryGetAccount(ent.Comp.AccessNumber, out var account))
            return;

        args.PushMarkup("The two below are for testing!", 3);
        args.PushMarkup(Loc.GetString("bank-card-examine-balance", ("balance", account.Balance)), 2); //todo remove this
        args.PushMarkup(Loc.GetString("bank-card-examine-salary", ("salary", account.Salary)), 1); //todo remove this
    }

    /// <summary>
    /// Create a new account. does nothing if run on the client.
    /// </summary>
    /// <returns>the Access number &amp; transfer number for the account</returns>
    public virtual (int AccessNumber, int TransferNumber) CreateNewAccount(string name)
    {
        return (0, 0);
    }

    public bool TryGetAccount(int accessNumber, [NotNullWhen(true)] out BankAccount? account)
    {
        account = null;

        var serverQuery = EntityQueryEnumerator<RemoteBankServerComponent>();
        if (!serverQuery.MoveNext(out var uid, out var server) ||
            !server.AccountDict.TryGetValue(accessNumber, out var bankAcc))
            return false;

        account = bankAcc;
        return true;

    }

    /// <summary>
    /// Set the name for an account
    /// </summary>
    /// <param name="accessNumber"></param>
    /// <param name="name"></param>
    public virtual void SetAccountName(int accessNumber, string name)
    {
        if (!TryGetAccount(accessNumber, out var account))
            return;

        account.Name = name;
    }

    public virtual void SetAccountSalary(int accessNumber, int salary)
    {
        if (!TryGetAccount(accessNumber, out var account))
            return;

        account.Salary = salary;
    }

    public virtual void SetAccountBalance(int accessNumber, int balance)
    {
        if (!TryGetAccount(accessNumber, out var account))
            return;

        account.Balance = balance;
    }

    /// <summary>
    /// Update the details on a bank card to reflect the details of a given account.
    /// </summary>
    /// <param name="card"></param>
    /// <param name="accessNumber"></param>
    public virtual void UpdateCardDetails(Entity<BankCardComponent> card, int accessNumber)
    {
        if (!TryGetAccount(accessNumber, out var account))
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
