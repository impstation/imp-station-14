using Content.Shared._Impstation.Heretic; // imp edit
using Content.Shared.Heretic.Prototypes;
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
        foreach (var uid in Uids)
        {
            var ghoul = new GhoulComponent()
            {
                HealthDivisor = 1.60, // imp edit
            };
            args.EntityManager.AddComponent(uid, ghoul, overwrite: true);
            args.EntityManager.EnsureComponent<MutedComponent>(uid);
        }
    }
}
