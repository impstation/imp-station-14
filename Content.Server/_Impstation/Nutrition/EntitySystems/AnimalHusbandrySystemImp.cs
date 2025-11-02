using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server._Impstation.Nutrition.EntitySystems;
public sealed class AnimalHusbandrySystemImp : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }

    public bool TryBreedWithTarget(EntityUid approacher, EntityUid approached, ReproductiveComponent? component = null)
    {
        if (!Resolve(approacher, ref component))
            return false;

        if (approacher == approached)
            return false;


        _adminLog.Add(LogType.Action, $"{ToPrettyString(approacher)} (carrier) and {ToPrettyString(approached)} (partner) successfully bred.");
        return true;
    }
}
