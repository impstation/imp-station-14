using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;

namespace Content.Client._Impstation.ItemSlotsMenu;

[UsedImplicitly]
public sealed class ItemSlotsBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private ItemSlotsMenu? _menu;

    public ItemSlotsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = new(Owner, this);
        _menu.OnClose += Close;

        // Open the menu, centered on the mouse
        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    public void SendItemSlotsEjectMessage(string e)
    {
        SendMessage(new ItemSlotButtonPressedEvent(e));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _menu?.Parent?.RemoveChild(_menu);
    }
}
