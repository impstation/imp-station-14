using Content.Server._Impstation.Dye.Components;
using Content.Server._Impstation.WashingMachine.Components;
using Content.Server._Impstation.WashingMachine.EntitySystems;
using Content.Server.Storage.Components;
using Content.Shared._Impstation.Dye;
using Robust.Shared.GameStates;

namespace Content.Server._Impstation.Dye.EntitySystems;

/// <summary>
/// Handles the dyeing and cleaning of dyeable entities.
/// </summary>
public sealed class DyeableSystem : EntitySystem
{
    [Dependency] private readonly WashingMachineSystem _washing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageComponent, WashEndEvent>(OnStorageWashed);

        SubscribeLocalEvent<DyeableComponent, GotDyedEvent>(OnDyed);
        SubscribeLocalEvent<DyedComponent, GotCleanedEvent>(OnCleaned);
        SubscribeLocalEvent<DyedComponent, ComponentGetState>(OnGetState);
    }

    private void OnStorageWashed(Entity<EntityStorageComponent> ent, ref WashEndEvent args)
    {
        var contents = new List<EntityUid>(ent.Comp.Contents.ContainedEntities);

        var dyes = new List<Entity<DyeComponent>>();
        var dyeables = new List<Entity<DyeableComponent>>();
        var cleaners = new List<Entity<DyeCleanerComponent>>();
        var dyeds = new List<Entity<DyedComponent>>();

        // making a list. checking it twice
        // theres gotta be a better way to do this but whatever
        foreach (var item in contents)
        {
            if (TryComp<DyeComponent>(item, out var dye))
            {
                dyes.Add((item, dye));
            }
            if (TryComp<DyeableComponent>(item, out var dyeable))
            {
                dyeables.Add((item, dyeable));
            }
            if (TryComp<DyeCleanerComponent>(item, out var cleaner))
            {
                cleaners.Add((item, cleaner));
            }
            if (TryComp<DyedComponent>(item, out var dyed))
            {
                dyeds.Add((item, dyed));
            }
        }

        foreach (var item in dyeables)
        {
            var ev = new GotDyedEvent(dyes);
            RaiseLocalEvent(item, ref ev);
        }

        // cancel if we have no cleaning agents or reagents
        if (!TryComp<WashingMachineComponent>(ent.Owner, out var wash)
        || _washing.GetReagents(ent.Owner).Item2 < wash.CleanerRequired
        && cleaners.Count == 0)
            return;

        foreach (var item in dyeds)
        {
            var ev = new GotCleanedEvent();
            RaiseLocalEvent(item, ref ev);

            // subtract cleaners if eligible
            if (wash != null && _washing.GetReagents(ent.Owner).Item2 >= wash.CleanerRequired)
                _washing.ChemicalUseReagent(ent.Owner, true);
            foreach (var cleanerItem in cleaners)
                if (cleanerItem.Comp.DeleteOnUse)
                    QueueDel(cleanerItem);
        }
    }

    private void OnDyed(Entity<DyeableComponent> ent, ref GotDyedEvent args)
    {
        // check for unique recipes first
        foreach (var (colorname, _) in ent.Comp.Recipes)
        {
            foreach (var dye in args.Dyes)
            {
                if (dye.Comp.Color == colorname)
                {
                    UniqueDye(ent, colorname);
                    return;
                }
            }
        }
        // not dying something that only accepts unique recipes
        if (ent.Comp.AcceptAnyColor)
        {
            var dyes = new List<Color>();
            if (TryComp<DyedComponent>(ent.Owner, out var dyed) && dyed.CurrentColor != Color.White)
                dyes.Add(dyed.CurrentColor);

            foreach (var dye in args.Dyes)
            {
                if (!Color.TryFromName(dye.Comp.Color, out var color))
                    color = Color.Transparent;
                dyes.Add(color);
            }
            var outColor = MixColors(dyes);

            EnsureComp<DyedComponent>(ent, out var outDyed);
            outDyed.CurrentColor = outColor;
            Dirty(ent.Owner, outDyed); // this is now bugged somehow. what the fuck!!!
        }
    }

    private void UniqueDye(Entity<DyeableComponent> ent, string colorname)
    {
        var spawned = Spawn(ent.Comp.Recipes[colorname], Transform(ent).Coordinates);
        var dyed = EnsureComp<DyedComponent>(spawned);
        dyed.CurrentColor = Color.White;
        dyed.OriginalEntity = MetaData(ent.Owner).EntityPrototype?.ID;

        QueueDel(ent.Owner);
        return;
    }

    private void OnCleaned(Entity<DyedComponent> ent, ref GotCleanedEvent _)
    {
        if (ent.Comp.OriginalEntity != null)
        {
            Spawn(ent.Comp.OriginalEntity, Transform(ent).Coordinates);
            Del(ent.Owner);
        }
        ent.Comp.CurrentColor = Color.White;
        Dirty(ent);
        // ideally we would remcomp here but doing so before we have a chance to get the component state breaks the sprite.
    }

    private static Color MixColors(List<Color> colors)
    {
        Vector4 hsl = new(0, 0, 0, 255);
        Vector4 outColor;
        foreach (var dye in colors)
        {
            outColor = Color.ToHsl(dye);
            hsl.X += outColor.X;
            hsl.Y += outColor.Y;
            hsl.Z += outColor.Z;
        }
        hsl.X /= colors.Count;
        hsl.Y /= colors.Count;
        hsl.Z /= colors.Count;
        return Color.FromHsl(hsl);
    }
    private void OnGetState(Entity<DyedComponent> ent, ref ComponentGetState args)
    {
        args.State = new DyedComponentState()
        {
            CurrentColor = ent.Comp.CurrentColor,
        };
    }

    /// <summary>
    /// Raised by <see cref="DyeableSystem"/> to dye an item.
    /// </summary>
    [ByRefEvent]
    public record struct GotDyedEvent(List<Entity<DyeComponent>> Dyes);

    /// <summary>
    /// Raised by <see cref="DyeableSystem"/> to clean a dyed item.
    /// </summary>
    [ByRefEvent]
    public record struct GotCleanedEvent;
}
