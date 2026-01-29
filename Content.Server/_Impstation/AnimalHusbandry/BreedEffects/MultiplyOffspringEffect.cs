using System.Linq;
using Content.Server._Impstation.AnimalHusbandry.Components;
using Content.Server._Impstation.Nutrition.EntitySystems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared._Impstation.EntityTable.Conditions;
using Content.Shared.EntityTable;

namespace Content.Server._Impstation.AnimalHusbandry.BreedEffects;

/// <summary>
/// Multiplies how many mobs will be born from a single birth
/// </summary>
public sealed partial class MultiplyOffspringEffect : BaseBreedEffect
{
    [DataField("extraOffspring")]
    public int ExtraOffspring = 0;

    private static AnimalHusbandrySystemImp? _animalHusbandrySystem;
    private static EntityTableSystem? _entityTableSystem;
    private ImpReproductiveComponent? _reproComp;

    public override void BirthEffect(EntityUid self, EntityUid offspring)
    {
        base.BirthEffect(self, offspring);
        if (_entManager == null) return;
        _entityTableSystem ??= _entManager.System<EntityTableSystem>();
        _animalHusbandrySystem ??= _entManager.System<AnimalHusbandrySystemImp>();
        _reproComp ??= _entManager.GetComponent<ImpReproductiveComponent>(self);

        for(int i = 0; i <= ExtraOffspring; ++i)
        {
            var partnerComp = _entManager.GetComponent<ImpReproductiveComponent>(_reproComp.PreviousPartner);

            var ctx = new EntityTableContext(new Dictionary<string, object>
            {
                { ValidPartnerCondition.PartnerContextKey, partnerComp.MobType },
            });
            var mobToBirth = _entityTableSystem.GetSpawns(_reproComp.PossibleInfants, ctx: ctx).ElementAt(0);

            var extraOffspring = _animalHusbandrySystem.SpawnNewMob(self, mobToBirth);

            if (_entManager.TryGetComponent<ImpInfantComponent>(extraOffspring, out var infantComp))
            {
                infantComp.Parent = self;
            }
        }
    }
}
