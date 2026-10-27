using Content.Shared._Impstation.CCVar;
using Content.Shared._Impstation.Interactions.Carving.Components;
using Content.Shared.Labels.EntitySystems;
using Robust.Shared.Configuration;

namespace Content.Shared._Impstation.Interactions.Carving.Systems;

public sealed partial class CarvingSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly LabelSystem _labelerSystem = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private bool _allowFullRename = false;

    private void UpdateMeta(Entity<CarvableComponent> ent, string name, string desc)
    {
        if (name != "")
            if (_allowFullRename)
                _metaSystem.SetEntityName(ent.Owner, name);
            else
                _labelerSystem.Label(ent, name);

        if (desc != "")
            _metaSystem.SetEntityDescription(ent.Owner, desc);
    }

    private bool IsEntityTextValid(Entity<CarvableComponent> ent, string name, string desc)
    {
        if (name.Length > _nameMaxLength || desc.Length > _descMaxLength)
        {
            _popupSystem.PopupEntity("You can't fit that much text!", ent.Owner, Popups.PopupType.Medium);
        }

        return true;
    }
}
