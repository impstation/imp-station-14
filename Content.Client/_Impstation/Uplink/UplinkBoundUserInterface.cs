using System.Linq;
using Content.Shared.Store;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Client._Impstation.Uplink;

/// <summary>
/// Bound user interface for uplinks.
/// </summary>
[UsedImplicitly]
public sealed class UplinkBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private readonly IPrototypeManager _prototypeManager = default!;

    [ViewVariables]
    private UplinkMenu? _menu;

    [ViewVariables]
    private string _search = string.Empty;

    [ViewVariables]
    private HashSet<ListingDataWithCostModifiers> _listings = new();

    /// <summary>
    /// Create the window and assign all the buttons and such.
    /// </summary>
    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<UplinkMenu>();

        _menu.Stylesheet = "Syndicate";

        _menu.OnListingButtonPressed += (_, listing) =>
        {
            SendMessage(new StoreBuyListingMessage(listing.ID));
        };

        _menu.OnCategoryButtonPressed += (_, category) =>
        {
            _menu.CurrentCategory = category;
            _menu?.UpdateListing();
        };

        _menu.OnWithdrawAttempt += (_, type, amount) =>
        {
            SendMessage(new StoreRequestWithdrawMessage(type, amount));
        };

        _menu.SearchTextUpdated += (_, search) =>
        {
            _search = search.Trim().ToLowerInvariant();
            UpdateListingsWithSearchFilter();
        };

        _menu.OnRefundAttempt += (_) =>
        {
            SendMessage(new StoreRequestRefundMessage());
        };
    }

    /// <summary>
    /// Update the store whenever we get the StoreUpdateState message.
    /// </summary>
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case StoreUpdateState msg:
                _listings = msg.Listings;

                _menu?.UpdateBalance(msg.Balance);

                UpdateListingsWithSearchFilter();
                _menu?.UpdateRefund(msg.AllowRefund);
                break;
        }
    }

    /// <summary>
    /// Update the store whenever with the given search filter.
    /// </summary>
    private void UpdateListingsWithSearchFilter()
    {
        if (_menu == null)
            return;

        var filteredListings = new HashSet<ListingDataWithCostModifiers>(_listings);
        if (!string.IsNullOrEmpty(_search))
        {
            filteredListings.RemoveWhere(listingData => !ListingLocalisationHelpers.GetLocalisedNameOrEntityName(listingData, _prototypeManager).Trim().ToLowerInvariant().Contains(_search) &&
                                                        !ListingLocalisationHelpers.GetLocalisedDescriptionOrEntityDescription(listingData, _prototypeManager).Trim().ToLowerInvariant().Contains(_search));
        }
        _menu.PopulateStoreCategoryButtons(filteredListings);
        _menu.UpdateListing(filteredListings.ToList());
    }
}
