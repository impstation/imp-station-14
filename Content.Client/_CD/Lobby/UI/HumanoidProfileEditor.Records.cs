// File is an extension of HumanoidProfileEditor.xaml.cs
using Content.Client._CD.Records.UI;
using Content.Shared._CD.Records;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private readonly RecordEditorGui _recordsTab;

    // Start CD - Character Records
    private void UpdateProfileRecords(PlayerProvidedCharacterRecords records)
    {
        if (Profile is null)
            return;
        Profile = Profile.WithCDCharacterRecords(records);
        IsDirty = true;
    }
    // End CD - Character Records

}
