using Content.Client._RMC14.Xenonids.UI;
using Content.Client.Message;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Content.Shared._RMC14.Xenonids.Plasma;

namespace Content.Client._RMC14.Xenonids.Evolution;

[UsedImplicitly]
public sealed class XenoEvolutionBui : BoundUserInterface
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;

    [ViewVariables]
    private XenoEvolutionWindow? _window;

    public XenoEvolutionBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<XenoEvolutionWindow>();

        Refresh();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        Refresh();
    }

    private void AddEvolution(EntProtoId evolutionId)
    {
        if (!_prototype.TryIndex(evolutionId, out var evolution))
            return;

        var control = new XenoChoiceControl();
        control.Set(evolution.Name, _sprite.Frame0(evolution));

        control.Button.OnPressed += _ =>
        {
            SendPredictedMessage(new XenoEvolveBuiMsg(evolutionId));
            Close();
        };

        _window?.EvolutionsContainer.AddChild(control);
    }

    public void Refresh()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out XenoEvolutionComponent? xeno))
            return;

        if (!EntMan.TryGetComponent(Owner, out XenoPlasmaComponent? plasma))
            return;

        _window.PointsLabel.Visible = xeno.Max > FixedPoint2.Zero;

        _window.EvolutionsContainer.DisposeAllChildren();
        foreach (var evolutionId in xeno.EvolvesToWithoutPoints)
        {
            AddEvolution(evolutionId);
        }

        if (plasma.Plasma >= xeno.Max)
        {
            foreach (var evolutionId in xeno.EvolvesTo)
            {
                AddEvolution(evolutionId);
            }
        }

        var points = plasma.Plasma;
        _window.PointsLabel.Text = $"Plasma: {(int) Math.Floor(points.Double())} / {xeno.Max}";
    }
}
