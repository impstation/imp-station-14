using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared._Impstation.Obvious;

namespace Content.Shared._Impstation.Obvious;

/// <summary>
/// Adds examine text to the entity, intentionally "obvious details".
/// Like, that's it. It's basic -- all it does is add the line to the attached entity.
/// This is particularly used for assigning players unique examine text.
/// </summary>
public sealed class ObviousExamineSystem : EntitySystem
{

    [Dependency] private readonly IEntityManager _entManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ObviousExamineComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<ObviousExamineComponent> entity, ref ExaminedEvent args)
    {
        if (entity.Comp.Lines.Count != 0)
        {
            var prefix = Loc.GetString("obvious-wearing", ("user", Identity.Entity(entity, EntityManager)), ("name", Identity.Name(entity, EntityManager))) + " ";
            foreach (var l in entity.Comp.Lines)
            {
                args.PushMarkup(prefix + Loc.GetString(l));
            }
        }
    }
}
