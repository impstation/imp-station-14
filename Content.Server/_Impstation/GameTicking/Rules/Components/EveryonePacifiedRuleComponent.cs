namespace Content.Server._Impstation.GameTicking.Rules;


/// <summary>
///     Gamerule that pacifies every player.
/// </summary>
[RegisterComponent, Access(typeof(EveryonePacifiedRuleSystem))]
public sealed partial class EveryonePacifiedRuleComponent : Component { }
