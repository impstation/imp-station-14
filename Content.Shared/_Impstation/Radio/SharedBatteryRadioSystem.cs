namespace Content.Shared._Impstation.Radio;

public abstract class SharedBatteryRadioSystem : EntitySystem
{
    public void ActivateSpeaker(EntityUid uid, bool activate = true)
    {
        if (activate)
        {
            EnsureComp<ActiveBatteryRadioComponent>(uid, out var active);
            active.UsingSpeaker = true;
        }
        else if (TryComp<ActiveBatteryRadioComponent>(uid, out var active) && active.UsingMicrophone)
            active.UsingSpeaker = false;
        else
            RemCompDeferred<ActiveBatteryRadioComponent>(uid);
    }
}
