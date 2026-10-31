using Content.Server.Forensics;
using Content.Server.Objectives.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DV.Paper;
using Content.Shared.DV.Recruiter;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Server.Body.Systems; // imp add

namespace Content.Server.DV.Recruiter;

/// <summary>
/// Handles bloodstream related code since that isn't in shared.
/// </summary>
public sealed class RecruiterPenSystem : SharedRecruiterPenSystem
{
    [Dependency] private readonly ForensicsSystem _forensics = default!;
    [Dependency] private readonly SolutionTransferSystem _transfer = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!; // imp add

    protected override void DrawBlood(EntityUid uid, Entity<SolutionComponent> dest, EntityUid user)
    {
        var bloodstream = CompOrNull<BloodstreamComponent>(user); // imp add, get blood color from BloodstreamSystem
        // how did you even use this mr plushie...
        if (bloodstream?.BloodSolution is not { } blood) // imp edit
            return;

        var desired = dest.Comp.Solution.AvailableVolume;
        // TODO: when bloodstream is shared put the transfer in shared so PopupClient is actually used, and this popup isnt needed
        if (desired == 0)
        {
            Popup.PopupEntity(Loc.GetString("recruiter-pen-prick-full", ("pen", uid)), user, user);
            return;
        }

        if (_transfer.Transfer(new SolutionTransferData(user, user, blood, uid, dest, desired)) != desired)
            return;

        // this is why you have to keep the pen safe, it has the dna of everyone you recruited!
        _forensics.TransferDna(uid, user, canDnaBeCleaned: false);

        if (TryComp<SignatureWriterComponent>(uid, out var signatureComp))
        {
            var bloodColor = _blood.GetBloodReferenceColor(bloodstream); // imp edit, get blood color from BloodstreamSystem
            signatureComp.Color = bloodColor;
        }

        Popup.PopupEntity(Loc.GetString("recruiter-pen-pricked", ("pen", uid)), user, user, PopupType.LargeCaution);
    }

    protected override void Recruit(Entity<RecruiterPenComponent> ent, EntityUid user)
    {
        // only increment count once if 1 person signs multiple papers
        if (!ent.Comp.Recruited.Add(user))
            return;

        if (!Mind.TryGetMind(user, out var userMindId, out _))
            return;

        if (ent.Comp.RecruiterMind is { } mindId &&
            mindId != userMindId &&
            Mind.TryGetObjectiveComp<RecruitingConditionComponent>(mindId, out var obj, null))
        {
            obj.Recruited++;
            Reward(ent, user);
        }
    }
    public void Reward(Entity<RecruiterPenComponent> ent, EntityUid user)
    {
        var pay = Spawn(ent.Comp.Currency);
        _hands.PickupOrDrop(user, pay, dropNear: true);
    }
}
