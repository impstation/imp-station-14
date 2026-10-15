using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.Interactions.Carving.UI;

[UsedImplicitly]
public sealed class CarvingBoundUserInterface : BoundUserInterface
{
    private CarvingInterface? _window;

    public CarvingBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CarvingInterface>();
        _window.Display(Owner);
        _window.UpdatePatternListing();
    }
}

