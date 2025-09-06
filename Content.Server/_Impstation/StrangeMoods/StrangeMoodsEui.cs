using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._Impstation.StrangeMoods;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.StrangeMoods;

public sealed class StrangeMoodsEui(StrangeMoodsSystem strangeMoodsSystem, EntityManager entityManager, IAdminManager manager) : BaseEui
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("strange-moods-eui");

    private List<StrangeMood> _moods = [];
    private ProtoId<SharedMoodPrototype>? _sharedMood;
    private EntityUid _target;

    public override EuiStateBase GetNewState()
    {
        return new StrangeMoodsEuiState(_moods, _sharedMood, entityManager.GetNetEntity(_target));
    }

    public void UpdateMoods(Entity<StrangeMoodsComponent> ent)
    {
        if (!IsAllowed())
            return;

        _moods = ent.Comp.Moods;
        _sharedMood = ent.Comp.SharedMoodPrototype;
        _target = ent;

        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not StrangeMoodsSaveMessage message)
            return;

        if (!IsAllowed())
            return;

        var uid = entityManager.GetEntity(message.Target);

        if (!entityManager.TryGetComponent<StrangeMoodsComponent>(uid, out var comp))
        {
            _sawmill.Warning($"Entity {entityManager.ToPrettyString(uid)} does not have StrangeMoodsComponent!");
            return;
        }

        strangeMoodsSystem.SetSharedMood((uid, comp), message.SharedMood);
        strangeMoodsSystem.SetMoods((uid, comp), message.Moods);
    }

    private bool IsAllowed()
    {
        var adminData = manager.GetAdminData(Player);

        if (adminData != null && adminData.HasFlag(AdminFlags.Moderator))
            return true;

        _sawmill.Warning($"Player {Player.UserId} tried to open / use strange moods UI without permission.");
        return false;

    }
}
