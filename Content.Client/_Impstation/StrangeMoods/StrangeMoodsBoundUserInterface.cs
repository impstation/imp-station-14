using Content.Shared._Impstation.StrangeMoods;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.StrangeMoods;

[UsedImplicitly]
public sealed class StrangeMoodsBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    [ViewVariables]
    private StrangeMoodsMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<StrangeMoodsMenu>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not StrangeMoodsBuiState msg)
            return;

        if (!_entMan.TryGetComponent<StrangeMoodsComponent>(Owner, out var comp))
            return;

        _menu?.Update(comp, msg);
    }
}
