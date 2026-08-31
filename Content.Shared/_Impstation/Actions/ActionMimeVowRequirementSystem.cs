using Content.Shared.Abilities.Mime;
using Content.Shared.Magic.Events;
using Content.Shared.Mind;
using Content.Shared.Popups;

namespace Content.Shared._Impstation.Actions;

/// <summary>
/// Makes an action require its user have an active mime vow to use.
/// </summary>
public sealed class ActionMimeVowRequirementSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionMimeVowRequirementComponent, BeforeCastSpellEvent>(OnBeforeCastSpell);
    }

    /// <summary>
    /// Check if the user has a mime vow and it's currently active. If not, cancel the action and create a popup.
    /// </summary>
    private void OnBeforeCastSpell(Entity<ActionMimeVowRequirementComponent> ent, ref BeforeCastSpellEvent args)
    {
        var mind = _mind.GetMind(args.Performer);

        if (mind == null)
            return;

        if (!_entManager.TryGetComponent<MimePowersComponent>(mind, out var mimeComponent))
        {
            args.Cancelled = true;
            _popup.PopupClient(Loc.GetString("mime-spell-failed"), args.Performer, args.Performer);
            return;
        }

        if (mimeComponent is { Enabled: true })
            return;

        args.Cancelled = true;
        _popup.PopupClient(Loc.GetString("mime-spell-failed-broken-vow"), args.Performer, args.Performer);
    }
}
