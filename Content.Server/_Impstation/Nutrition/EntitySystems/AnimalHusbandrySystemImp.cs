using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared._Impstation.Nutrition.Components;
using Content.Shared.Database;
using Content.Shared.Interaction.Components;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Nutrition.EntitySystems;
public sealed class AnimalHusbandrySystemImp : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    Dictionary<EntityUid, ImpReproductiveComponent> _mobsWaiting = new Dictionary<EntityUid, ImpReproductiveComponent>();

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var reproComp in _mobsWaiting)
        {
            if(reproComp.Value == null)
            {
                _mobsWaiting.Remove(reproComp.Key);
                continue;
            }

            if (_time.CurTime > reproComp.Value.EndPregnancy)
            {
                reproComp.Value.Pregnant = false;
                Birth(reproComp.Key, reproComp.Value);
                _mobsWaiting.Remove(reproComp.Key);
                _adminLog.Add(LogType.Action, $"A mob has given birth!");
            }
        }
    }

    public bool TryBreedWithTarget(EntityUid approacher, EntityUid approached)
    {
        // Realistically this should never return false but it's just here for the moment
        if (!_entManager.TryGetComponent<ImpReproductiveComponent>(approacher, out var component))
            return false;

        // Same with this one but i'm paranoid
        if (!_entManager.TryGetComponent<ImpReproductiveComponent>(approached, out var partnerComp))
            return false;

        if (!Resolve(approacher, ref component))
            return false;

        if (approacher == approached)
            return false;

        partnerComp.Pregnant = true;
        partnerComp.EndPregnancy = _time.CurTime + partnerComp.PregnancyLength;

        _mobsWaiting.Add(approached, partnerComp);

        _popup.PopupEntity("BREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBREDBRED", approached);

        _hunger.ModifyHunger(approacher, -component.HungerPerBirth);
        _hunger.ModifyHunger(approached, -partnerComp.HungerPerBirth);

        _adminLog.Add(LogType.Action, $"{ToPrettyString(approacher)} (carrier) and {ToPrettyString(approached)} (partner) successfully bred.");
        return true;
    }

    public bool ReadyToBreed(ImpReproductiveComponent reproComp)
    {
        if (reproComp.Pregnant)
            return false;

        if (reproComp.NextSearch > _time.CurTime && !reproComp.PartnerInMind)
            return false;

        return true;
    }

    public void Birth(EntityUid entity, ImpReproductiveComponent? reproComp = null)
    {
        if (TryComp<InteractionPopupComponent>(entity, out var interactionPopup))
            _audio.PlayPvs(interactionPopup.InteractSuccessSound, entity);

        _popup.PopupEntity("BIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTHBIRTH", entity);
    }
}
