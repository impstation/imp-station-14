using Content.Server.Polymorph.Systems;
using Content.Server.Actions;
using Content.Server.Popups;
using Content.Shared._Wizden.Geras;
using Content.Shared.Humanoid;
using Robust.Shared.Player;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;

namespace Content.Server.Geras;

/// <inheritdoc/>
public sealed class GerasSystem : SharedGerasSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GerasComponent, MorphIntoGeras>(OnMorphIntoGeras);
        SubscribeLocalEvent<GerasComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, GerasComponent component, MapInitEvent args)
    {
        // try to add geras action to non geras
        if (!component.NoAction)
        {
            _actionsSystem.AddAction(uid, ref component.GerasActionEntity, component.GerasAction);
        }
    }

    private void OnMorphIntoGeras(EntityUid uid, GerasComponent component, MorphIntoGeras args)
    {

        var ent = _polymorphSystem.PolymorphEntity(uid, component.GerasPolymorphId);

        if (!ent.HasValue)
            return;

        if (_entities.TryGetComponent<StorageComponent>(uid, out var storage))
        {
            //Transfer
            var gerasStorage = EnsureComp<StorageComponent>(ent.Value);

            _storage.TransferEntities(uid, ent.Value, sourceComp: storage, targetComp: gerasStorage);
        }

        var skinColor = Color.Green;

        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanComp))
        {
            skinColor = humanComp.SkinColor;
        }

        if (TryComp<AppearanceComponent>(ent, out var appearanceComp))
        {
            _appearance.SetData(ent.Value, GeraColor.Color, skinColor, appearanceComp);
        }

        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-others", ("entity", ent.Value)), ent.Value, Filter.PvsExcept(ent.Value), true);
        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-user"), ent.Value, ent.Value);

        args.Handled = true;
    }
}
