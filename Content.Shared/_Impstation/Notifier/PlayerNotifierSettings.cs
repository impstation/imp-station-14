using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Notifier;

[Serializable, NetSerializable]
public sealed class PlayerNotifierSettings
{
    public string Freetext;
    public bool Enabled;

    public PlayerNotifierSettings()
    {
        Freetext = string.Empty;
        Enabled = false;
    }

    public PlayerNotifierSettings(string freetext, bool enabled)
    {
        Freetext = freetext;
        Enabled = enabled;
    }

}
