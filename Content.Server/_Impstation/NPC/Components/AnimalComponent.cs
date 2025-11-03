using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._Impstation.NPC.Components;
[RegisterComponent]
public sealed partial class AnimalComponent : Component
{
    // Max amount of time our animal can spend on the chase
    [DataField("maxTimeChasing")]
    public TimeSpan MaxChaseTime = TimeSpan.FromSeconds(20);

    // The max amount of time our animal can rest before the next chase
    [DataField("maxRestTime")]
    public long MaxRestTime = 60;

    // The minimum amount of time our animal can rest before the next chase
    [DataField("minRestTime")]
    public long MinRestTime = 45;

    // The time we will end our chase at
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndChase = TimeSpan.Zero;

    // The time we will once again begin the hunt
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextChase = TimeSpan.Zero;

    // How's our animal feeling?
    [ViewVariables(VVAccess.ReadWrite)]
    public AnimalMood CurrentMood = AnimalMood.Bored;

    public bool Chasing = false;
}

public enum AnimalMood
{
    Tired, // Animal needs to go lay down
    Bored, // Animal is looking for something to do
    Chasing, // Couriers beware
    Fetching // Animal is playing fetch
}
