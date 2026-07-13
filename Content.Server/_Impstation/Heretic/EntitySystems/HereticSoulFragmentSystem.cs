using Content.Server.Heretic.Components;
using Content.Shared.Examine;
using Content.Shared.Heretic;


namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticSoulFragmentSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HereticSoulFragmentComponent, ExaminedEvent>(OnExamined);
    }

private void OnExamined(Entity<HereticSoulFragmentComponent> ent, ref ExaminedEvent args)
    {
        if(TryComp<HereticComponent>(args.Examiner, out _))
        {
            args.PushMarkup(markup: Loc.GetString(ent.Comp.Message));
        }
    }
}
