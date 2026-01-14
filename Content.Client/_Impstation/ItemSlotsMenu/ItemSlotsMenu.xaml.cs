using System.Linq;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Containers.ItemSlots;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._Impstation.ItemSlotsMenu;

public sealed partial class ItemSlotsMenu: RadialMenu
{
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly ItemSlotsSystem _itemSlots;

    private const string MainString = "Main";

    public event Action<string>? ItemSlotEjectMessageAction;

    public ItemSlotsMenu(EntityUid owner, ItemSlotsBoundUserInterface bui)
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        _itemSlots = _entManager.System<ItemSlotsSystem>();

        // Find the main radial container
        var main = FindControl<RadialContainer>(MainString);

        if (!_entManager.TryGetComponent<ItemSlotsComponent>(owner, out var slotsComp))
            return;

        if (slotsComp.Slots.All(x => x.Value.Item == null))
            Close();

        foreach (var itemSlot in slotsComp.Slots)
        {
            if (_playerManager.LocalSession == null)
                continue;

            var item = itemSlot.Value.Item;
            string itemName;

            if (item == null)
                continue;

            if (!_entManager.TryGetComponent<MetaDataComponent>(item, out var metadata))
                continue;

            itemName = metadata.EntityName;

            var button = new ItemSlotButton()
            {
                SetSize = new Vector2(64f, 64f),
                ToolTip = itemName,
            };

            if (!_entManager.TryGetComponent<SpriteComponent>(item, out var sprite))
                continue;

            if (sprite.Icon == null)
                continue;

            var tex = new TextureRect()
            {
                VerticalAlignment = VAlignment.Center,
                HorizontalAlignment = HAlignment.Center,
                Texture = sprite.Icon?.Default,
                TextureScale = new Vector2(2f, 2f),
            };

            button.AddChild(tex);
            main.AddChild(button);

            button.OnButtonUp += _ =>
            {
                ItemSlotEjectMessageAction?.Invoke(itemSlot.Key);
                _itemSlots.TryEjectToHands(item.Value, itemSlot.Value, owner);
                Close();
            };
        }

        ItemSlotEjectMessageAction += bui.SendItemSlotsEjectMessage;
    }
}

public sealed class ItemSlotButton : RadialMenuButtonWithSector
{
    public ItemSlotButton()
    {

    }
}
