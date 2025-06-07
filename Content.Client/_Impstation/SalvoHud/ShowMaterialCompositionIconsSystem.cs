using Content.Client.Overlays;
using Content.Shared._Impstation.SalvoHud;
using Content.Shared.Actions;
using Content.Shared.Materials;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Impstation.SalvoHud;

/// <summary>
/// This handles...
/// </summary>
public sealed class ShowMaterialCompositionIconsSystem : EquipmentHudSystem<ShowMaterialCompositionIconsComponent>
{

    //todo fixedPrice showing as well
    //todo salvager rtb / sell / scrap marker?

    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    //storing a bunch of state inside a system yaaaaay
    //this sucks but I can't really do this any other way afaik
    private SalvohudScanState _state = SalvohudScanState.Idle;
    private float _currMinRange = 0;
    private float _currMaxRange = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhysicalCompositionComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        SubscribeLocalEvent<ShowMaterialCompositionIconsComponent, ActivateSalvoHudEvent>(OnActivateSalvoHudEvent);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<ShowMaterialCompositionIconsComponent>();

        while (query.MoveNext(out var comp))
        {
            switch (comp.CurrState)
            {
                default:
                case SalvohudScanState.Idle:
                    comp.Accumulator = 0;
                    _state = SalvohudScanState.Idle;
                    break;

                case SalvohudScanState.In:
                    comp.Accumulator += frameTime;
                    comp.CurrMaxRange = comp.MaxRange * (comp.Accumulator / comp.InPeriod);
                    _state = SalvohudScanState.In;

                    if (!(comp.Accumulator > comp.InPeriod))
                        break;

                    comp.Accumulator = 0;
                    comp.CurrState = SalvohudScanState.Active;
                    break;

                case SalvohudScanState.Active:
                    comp.Accumulator += frameTime;
                    comp.CurrMaxRange = comp.MaxRange;
                    _state = SalvohudScanState.Active;

                    if (!(comp.Accumulator > comp.ActivePeriod))
                        break;

                    comp.Accumulator = 0;
                    comp.CurrState = SalvohudScanState.Out;
                    break;

                case SalvohudScanState.Out:
                    comp.Accumulator += frameTime;
                    comp.CurrMinRange = comp.MaxRange * (comp.Accumulator / comp.OutPeriod);
                    _state = SalvohudScanState.Out;

                    if (!(comp.Accumulator > comp.OutPeriod))
                        break;

                    comp.CurrMaxRange = 0;
                    comp.CurrMinRange = 0;
                    comp.Accumulator = 0;
                    comp.CurrState = SalvohudScanState.Idle;
                    break;
            }

            //this sucks bad
            _state = comp.CurrState;
            _currMinRange = comp.CurrMinRange;
            _currMaxRange = comp.CurrMaxRange;
        }
    }

    private void OnActivateSalvoHudEvent(Entity<ShowMaterialCompositionIconsComponent> ent, ref ActivateSalvoHudEvent args)
    {
        ent.Comp.CurrState = SalvohudScanState.In;
    }

    //todo this is really fucking broken for some reason
    private void OnGetStatusIconsEvent(Entity<PhysicalCompositionComponent> entity, ref GetStatusIconsEvent args)
    {
        if (!IsActive || _state == SalvohudScanState.Idle || _playerMan.LocalEntity is not {} player)
            return;

        var diff = _xform.GetWorldPosition(entity) - _xform.GetWorldPosition(player);
        var dist = diff.Length();

        if (dist > _currMaxRange || dist < _currMinRange)
            return;

        foreach (var (id, _) in entity.Comp.MaterialComposition)
        {
            if (_protoMan.TryIndex<MaterialCompositionIconPrototype>(id, out var proto))
                args.StatusIcons.Add(proto);
        }
    }
}
