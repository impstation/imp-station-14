using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Whitelist;

namespace Content.Shared._Impstation.Nutrition.Components;
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ImpReproductiveComponent : Component
{

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MinBreedAttemptInterval = TimeSpan.FromSeconds(45);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaxBreedAttemptInterval = TimeSpan.FromSeconds(60);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MinSearchAttemptInterval = TimeSpan.FromSeconds(10);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaxSearchAttemptInterval = TimeSpan.FromSeconds(30);

    [DataField("partnerWhiteList", required: true)]
    public EntityWhitelist PartnerWhiteList = default!;

    // What species are we?
    [DataField("reproductiveGroup", required: true)]
    public string ReproductiveGroup = "MobNone";

    [DataField("hungerPerBirth", required: true)]
    public int HungerPerBirth = 75;

    [DataField("pregnancyLength", required: true)]
    public TimeSpan PregnancyLength = TimeSpan.FromSeconds(60);

    public bool Pregnant = false;
    public bool PartnerInMind = false;

    public TimeSpan EndPregnancy = TimeSpan.Zero;
    public TimeSpan NextSearch = TimeSpan.Zero;
}
