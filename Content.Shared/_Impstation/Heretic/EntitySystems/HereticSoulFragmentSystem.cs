using Content.Shared.Heretic.Components;
using Content.Shared.Examine;


namespace Content.Shared.Heretic.EntitySystems;

public sealed partial class HereticSoulFragmentSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HereticSoulFragmentComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<HereticSoulFragmentComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<HereticComponent>(args.Examiner))
        {
            args.PushMarkup(markup: Loc.GetString(ent.Comp.Message));
        }
    }
}
