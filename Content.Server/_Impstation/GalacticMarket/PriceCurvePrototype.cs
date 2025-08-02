using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.GalacticMarket;

/// <summary>
/// This prototype determines the price scaling for a given prototype
/// </summary>
[Prototype()]
public sealed partial class PriceCurvePrototype : IPrototype
{
    /// <summary>
    /// the ID for this prototype. should be the same as whatever proto you want to scale the price of.
    /// </summary>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public PriceCurveType Type = PriceCurveType.None;

    [DataField]
    public double MaxAllowedValue = double.MaxValue;

    public float GetPriceMult(double soldValue)
    {
        var frac = soldValue / MaxAllowedValue;
        switch (Type)
        {
            default:
            case PriceCurveType.None:
                return 1f;
            case PriceCurveType.Hard:
                return soldValue > MaxAllowedValue ? 0f : 1f;
            case PriceCurveType.RectifiedLinear:
                if (frac < 0.5)
                    return 1f;
                return (float) ((frac - 0.5) / 0.5);
            case PriceCurveType.RectifiedSquared:
                if (frac < 0.5)
                    return 1f;
                var amount = (frac - 0.5) / 0.5;
                return (float) (amount * amount);
            case PriceCurveType.Linear:
                return (float) (soldValue / MaxAllowedValue);
            case PriceCurveType.Squared:
                return (float) ((soldValue / MaxAllowedValue) * (soldValue / MaxAllowedValue));
        }
    }
}

//currently too simple; the ideal for this is a full bezier curve setup for complete control over this all, but it works for now
public enum PriceCurveType : byte
{
    None, //no price curve
    Hard, //jumps straight to 0 when it crosses the line
    RectifiedLinear, //no price reduction until halfway, then linearly ramps up to 100%
    RectifiedSquared, //no price reduction until halfway, then squared-ly ramps up to 100%
    Linear, //linear reduction w/ sell value
    Squared, //squared-ly reduction w/ sell value
}
