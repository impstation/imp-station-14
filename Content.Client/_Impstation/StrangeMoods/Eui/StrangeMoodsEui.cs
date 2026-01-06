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
        _strangeMoodUi.OnGenerate += GenerateMood;
        _strangeMoodUi.OnSave += SaveMoods;
        _strangeMoodUi.OnSharedSelected += GetSharedMood;
    }

    private void GenerateMood()
    {
        SendMessage(new StrangeMoodsGenerateRequestMessage(_target));
    }

    private void SaveMoods()
    {
        var newMoods = _strangeMoodUi.GetMoods();
        var sharedMood = _strangeMoodUi.GetSharedMood();
        SendMessage(new StrangeMoodsSaveMessage(newMoods, sharedMood?.ProtoId, _target));
        _strangeMoodUi.SetAllMoods(newMoods, sharedMood);
    }

    private void GetSharedMood(SharedMood mood)
    {
        SendMessage(new StrangeMoodsSharedRequestMessage(mood.ProtoId));
    }

    public override void Opened()
    {
        _strangeMoodUi.OpenCentered();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case (StrangeMoodsGenerateSendMessage generateSent):
            {
                _strangeMoodUi.AddNewMood(generateSent.Mood);
                break;
            }
            case (StrangeMoodsSharedSendMessage sharedSent):
            {
                _strangeMoodUi.SetSharedMood(sharedSent.Mood);
                break;
            }
        }
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not StrangeMoodsEuiState s)
            return;

        _target = s.Target;
        _strangeMoodUi.SetAllMoods(s.Moods, s.SharedMood);
        _strangeMoodUi.PopulateDropDown(s.AllSharedMoods, s.SharedMood);
    }
}
