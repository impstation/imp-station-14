using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Keyring.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class KeyringComponent : Component
{
    /// <summary>
    /// Time it takes for the keyring to open a door.
    /// </summary>
    [DataField]
    public float OpenTime = 10.0f;

    /// <summary>
    /// What sound to play while the keyring is being used on the door.
    /// </summary>
    [DataField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/_Impstation/Items/keyring_fumble.ogg");


    /// <summary>
    /// What sound to play once the keyring opens the door.
    /// </summary>
    [DataField]
    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/_Impstation/Items/keyring_unlock.ogg");
}

