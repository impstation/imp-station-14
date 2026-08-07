namespace Content.Shared.Lube;

[RegisterComponent]
public sealed partial class LubedComponent : Component
{
    [DataField("slipsLeft"), ViewVariables(VVAccess.ReadWrite)]
    public int SlipsLeft;

    [DataField("slipStrength"), ViewVariables(VVAccess.ReadWrite)]
    public int SlipStrength;

   /// <summary>
   /// Imp addition. Controls if "lubed" is added to the start of lubed entities' names.
   /// </summary>
   [DataField]
   public bool ApplyNamePrefix = true;

}
