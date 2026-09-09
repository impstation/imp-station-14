// Box TODO: revert this shit the milisecond floorlights get refactored this sucks
using Content.Shared.Destructible;
using Content.Shared.Light.Components;
using Content.Shared.Tag; // Box Change: imp floorlights
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes; // Box Change: imp floorlights

namespace Content.Shared.Light.EntitySystems;

public abstract class SharedLightBulbSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    // Start Box Change: prevent imp floorlights from infinitely recycling broken lights
    [Dependency] private readonly TagSystem _tags = default!;
    private HashSet<ProtoId<TagPrototype>> _tagsToRemove = [ // I fucjing hate this
        "LightBlue",
        "LightBlack",
        "LightCyan",
        "LightDim",
        "LightExterior",
        "LightGreen",
        "LightLed",
        "LightNormal",
        "LightOld",
        "LightOrange",
        "LightPink",
        "LightRed",
        "LightService",
        "LightSodium",
        "LightUv",
        "LightWarm",
        "LightYellow"
        ];
    // End Box Change

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightBulbComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LightBulbComponent, LandEvent>(HandleLand);
        SubscribeLocalEvent<LightBulbComponent, BreakageEventArgs>(OnBreak);
    }

    private void OnInit(EntityUid uid, LightBulbComponent bulb, ComponentInit args)
    {
        // update default state of bulbs
        UpdateAppearance(uid, bulb);
    }

    private void HandleLand(EntityUid uid, LightBulbComponent bulb, ref LandEvent args)
    {
        PlayBreakSound(uid, bulb);
        SetState(uid, LightBulbState.Broken, bulb);
        SetBrokenTags(uid); // Box Change: imp floorlights
    }

    private void OnBreak(EntityUid uid, LightBulbComponent component, BreakageEventArgs args)
    {
        SetState(uid, LightBulbState.Broken, component);
        SetBrokenTags(uid); // Box Change: imp floorlights
    }

    /// <summary>
    ///     Set a new color for a light bulb and raise event about change
    /// </summary>
    public void SetColor(EntityUid uid, Color color, LightBulbComponent? bulb = null)
    {
        if (!Resolve(uid, ref bulb) || bulb.Color.Equals(color))
            return;

        bulb.Color = color;
        Dirty(uid, bulb);
        UpdateAppearance(uid, bulb);
    }

    /// <summary>
    ///     Set a new state for a light bulb (broken, burned) and raise event about change
    /// </summary>
    public void SetState(EntityUid uid, LightBulbState state, LightBulbComponent? bulb = null)
    {
        if (!Resolve(uid, ref bulb) || bulb.State == state)
            return;

        bulb.State = state;
        Dirty(uid, bulb);
        UpdateAppearance(uid, bulb);
    }

    public void PlayBreakSound(EntityUid uid, LightBulbComponent? bulb = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref bulb))
            return;

        _audio.PlayPredicted(bulb.BreakSound, uid, user: user);
    }

    private void UpdateAppearance(EntityUid uid, LightBulbComponent? bulb = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref bulb, ref appearance, logMissing: false))
            return;

        // try to update appearance and color
        _appearance.SetData(uid, LightBulbVisuals.State, bulb.State, appearance);
        _appearance.SetData(uid, LightBulbVisuals.Color, bulb.Color, appearance);
    }

    //Start Box Change: prevent imp floorlights from infinitely recycling broken lights
    private void SetBrokenTags(EntityUid uid)
    {
        if (!TryComp<TagComponent>(uid, out var tags))
            return;
        _tags.RemoveTags(uid, _tagsToRemove);
    }
    //End Box Change
}
