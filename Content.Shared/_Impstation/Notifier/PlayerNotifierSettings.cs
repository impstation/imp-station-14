using Content.Shared._Impstation.CCVar;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
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

    public void EnsureValid(IConfigurationManager configManager,
        IPrototypeManager prototypeManager)
    {
        var maxLength = configManager.GetCVar(ImpCCVars.NotifierFreetextMaxLength);
        Freetext = Freetext.Trim();
        if (Freetext.Length > maxLength)
            Freetext = Freetext.Substring(0, maxLength);
    }

}
