using Content.Client.Eui;
using Content.Shared._Impstation.StrangeMoods;
using Content.Shared.Eui;

namespace Content.Client._Impstation.StrangeMoods.Eui;

public sealed class StrangeMoodsEui : BaseEui
{
    private readonly StrangeMoodUi _strangeMoodUi;
    private NetEntity _target;

    public StrangeMoodsEui()
    {
        _strangeMoodUi = new StrangeMoodUi();
        _strangeMoodUi.OnSave += SaveMoods;
    }

    private void SaveMoods()
    {
        var newMoods = _strangeMoodUi.GetMoods();
        //var toggle = _strangeMoodUi.ShouldFollowShared();
        SendMessage(new StrangeMoodsSaveMessage(newMoods, "Thaven", _target)); // TODO: MAKE THIS SEND THE ACTUAL SHARED MOOD
        _strangeMoodUi.SetMoods(newMoods);
    }

    public override void Opened()
    {
        _strangeMoodUi.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not StrangeMoodsEuiState s)
            return;

        _target = s.Target;
        //_strangeMoodUi.SetFollowShared(s.FollowsShared);
        _strangeMoodUi.SetMoods(s.Moods);
    }
}
