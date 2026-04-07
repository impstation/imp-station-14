using System.Linq;
using Content.Shared.Store;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Impstation.Heretic.Store;

[UsedImplicitly]
public sealed class HereticStoreBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private IPrototypeManager _prototypeManager = default!;

    [ViewVariables]
    private HereticMenu? _menu;

    [ViewVariables]
    private string _search = string.Empty;

    [ViewVariables]
    private HashSet<ListingDataWithCostModifiers> _listings = new();

    /// <summary>
    /// Open a window with styling and buttons and such.
    /// </summary>
    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<HereticMenu>();

        _menu.Stylesheet = "Heretic";

        _menu.OnListingButtonPressed += (_, listing) =>
        {
            SendMessage(new StoreBuyListingMessage(listing.ID));
        };

        _menu.OnCategoryButtonPressed += (_, category) =>
        {
            _menu.CurrentCategory = category;
            _menu?.UpdateListing();
        };

        _menu.SearchTextUpdated += (_, search) =>
        {
            _search = search.Trim().ToLowerInvariant();
            UpdateListingsWithSearchFilter();
        };
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case StoreUpdateState msg:
                _listings = msg.Listings;

                _menu?.UpdateBalance(msg.Balance);

                UpdateListingsWithSearchFilter();
                _menu?.SetFooterVisibility(msg.ShowFooter);
                break;
        }
    }

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
