using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Marker component on soul fragment items. Enables the restore-effigy do-after logic
/// when a Slasher with a destroyed effigy uses the fragment in hand.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlasherSoulFragmentComponent : Component
{
	/// <summary>
	/// Channel duration required to restore a destroyed effigy by using this fragment.
	/// </summary>
	[DataField]
	public TimeSpan RestoreEffigyDoAfterDuration { get; set; } = TimeSpan.FromSeconds(30);
}
