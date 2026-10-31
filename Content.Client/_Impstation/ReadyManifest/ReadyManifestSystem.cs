using Content.Shared._Impstation.ReadyManifest;

namespace Content.Client._Impstation.ReadyManifest;

public sealed class ReadyManifestSystem : SharedReadyManifestSystem
{
    public void RequestReadyManifest()
    {
        RaiseNetworkEvent(new RequestReadyManifestMessage());
    }
}
