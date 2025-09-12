using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Service;

/// <summary>
///     Prototype for a service job.
///     This job contains a description of a station event, which
///     the hospitality director can choose to initiate.
/// </summary>
// TODO: nanochat app?
[Prototype]
public sealed partial class ServiceJobPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Description of the event.
    /// </summary>
    [DataField]
    public LocId Description = string.Empty;

    /// <summary>
    ///     Announcement to be sent over comms when the event is selected.
    /// </summary>
    [DataField(required: true)]
    public LocId SelectAnnounce = string.Empty;

    /// <summary>
    ///     Timer from when the event is selected to when the event is announced over station communications.
    /// </summary>
    [DataField]
    public TimeSpan Timer = TimeSpan.FromMinutes(45);

    /// <summary>
    ///     Announcement to be sent over station-wide communications when the timer expires.
    /// </summary>
    [DataField(required: true)]
    public LocId StartAnnounce = string.Empty;

    /// <summary>
    ///     Optional sprite representing this job.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Sprite;
}
