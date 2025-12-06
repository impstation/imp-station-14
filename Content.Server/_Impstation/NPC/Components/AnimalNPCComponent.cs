using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._Impstation.NPC.Components;

/// <summary>
/// The Component intended for handling an Animals mood and Actions
/// </summary>
[RegisterComponent]
public sealed partial class AnimalNPCComponent : Component
{
    // Max amount of time our animal can spend on the chase
    [DataField("maxTimeChasing")]
    public TimeSpan MaxChaseTime = TimeSpan.FromSeconds(20);

    // The max amount of time our animal can rest before the next chase
    [DataField("maxRestTime")]
    public int MaxRestTime = 60;

    // The minimum amount of time our animal can rest before the next chase
    [DataField("minRestTime")]
    public int MinRestTime = 45;

    // The time we will end our chase at
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndChase = TimeSpan.Zero;

    // The time we will once again be allowed to begin the hunt
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextChase = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndRest = TimeSpan.Zero;

    // How's our animal feeling?
    [ViewVariables(VVAccess.ReadWrite)]
    public AnimalMood CurrentMood = AnimalMood.Bored;
}

public enum AnimalMood
{
    Tired, // Animal needs to go lay down
    Resting, // *Snore* Mimimimimimimi
    Bored, // Animal is looking for something to do
    Chasing, // Couriers beware
    Fetching // Animal is playing fetch
}
