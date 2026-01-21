namespace Content.Shared._Impstation.Fishing.Events;

public sealed class FishingAttemptEvent(EntityUid fishingPool) : CancellableEntityEventArgs
{
    public EntityUid FishingPool = fishingPool;
}
