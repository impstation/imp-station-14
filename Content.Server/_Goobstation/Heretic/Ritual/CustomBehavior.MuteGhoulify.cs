using Content.Shared._Impstation.Heretic.Components;
using Content.Shared._Impstation.Heretic; // imp edit
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;

namespace Content.Server.Heretic.Ritual;

public sealed partial class RitualMuteGhoulifyBehavior : RitualSacrificeBehavior
{
    public override bool Execute(RitualData args, out string? outstr)
    {
        return base.Execute(args, out outstr);
    }

    public override void Finalize(RitualData args)
    {
        var popup = args.EntityManager.System<SharedPopupSystem>(); //imp add popup system

        foreach (var uid in Uids)
        {
            if (args.EntityManager.HasComponent<NoGhoulComponent>(uid)) // imp add noghoul component
            {
                popup.PopupEntity(Loc.GetString("heretic-ghoul-unghoulable"), args.Performer, args.Performer, PopupType.MediumCaution);
                continue;
            }

            var ghoul = new GhoulComponent()
            {
                HealthDivisor = 1.60, // imp edit
            };
            args.EntityManager.AddComponent(uid, ghoul, overwrite: true);
            args.EntityManager.EnsureComponent<MutedComponent>(uid);
        }
    }
}
