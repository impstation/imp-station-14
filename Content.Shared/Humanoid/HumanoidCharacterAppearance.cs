using System.Linq;
using System.Numerics;
using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidCharacterAppearance : ICharacterAppearance, IEquatable<HumanoidCharacterAppearance>
{
    [DataField]
    public Color EyeColor { get; set; } = Color.Black;

    [DataField]
    public Color SkinColor { get; set; } = Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings { get; set; } = new();

    public HumanoidCharacterAppearance(
        Color eyeColor,
        Color skinColor,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings)
    {
        EyeColor = ClampColor(eyeColor);
        SkinColor = ClampColor(skinColor);
        Markings = markings;
    }

    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.EyeColor, other.SkinColor, new(other.Markings))
    {

    }

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(newColor, SkinColor, Markings);
    }

    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new(EyeColor, newColor, Markings);
    }

    public HumanoidCharacterAppearance WithMarkings(Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> newMarkings)
    {
        return new(EyeColor, SkinColor, newMarkings);
    }

    public static HumanoidCharacterAppearance DefaultWithSpecies(ProtoId<SpeciesPrototype> species, Sex sex)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var speciesPrototype = protoMan.Index<SpeciesPrototype>(species);
        var skinColoration = protoMan.Index(speciesPrototype.SkinColoration).Strategy;
        var skinColor = skinColoration.InputType switch
        {
            SkinColorationStrategyInput.Unary => skinColoration.FromUnary(speciesPrototype.DefaultHumanSkinTone),
            SkinColorationStrategyInput.Color => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
            _ => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
        };

        var appearance = new HumanoidCharacterAppearance(
            Color.Black,
            skinColor,
            new()
        );
        return EnsureValid(appearance, species, sex);
    }

    private static IReadOnlyList<Color> _realisticEyeColors = new List<Color>
    {
        Color.Brown,
        Color.Gray,
        Color.Azure,
        Color.SteelBlue,
        Color.Black
    };

    public static HumanoidCharacterAppearance Random(string species, Sex sex)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        // TODO: Add random markings

        var newEyeColor = random.Pick(_realisticEyeColors);

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var skinType = protoMan.Index<SpeciesPrototype>(species).SkinColoration;
        var strategy = protoMan.Index(skinType).Strategy;

        // IMP: we dont need any of this lol
        /* var newSkinColor = strategy.InputType switch
        {
            SkinColorationStrategyInput.Unary => strategy.FromUnary(random.NextFloat(0f, 100f)),
            SkinColorationStrategyInput.Color => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
            _ => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
        };

        return new HumanoidCharacterAppearance(newEyeColor, newSkinColor, new());

        // helper functions:
        // (this one is upstream)
        float RandomizeColor(float channel)
        {
            return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
        }

        // imp:
        List<Color> GetComplementaryColors(Color color, double angle)
        {
            var hsl = Color.ToHsl(color);

            // sorry about how messy these are, but to get all random values we need to reroll for positive and negative HSL
            var hVal = hsl.X + angle;
            hVal = hVal >= 0.360 ? hVal - 0.360 : hVal;
            var positiveHSL = new Vector4((float)hVal, MathHelper.Clamp01(hsl.Y + random.Next(-20, 0) / 100f), MathHelper.Clamp01(hsl.Z + random.Next(-15, 15) / 100f), hsl.W);

            var hVal1 = hsl.X - angle;
            hVal1 = hVal1 <= 0 ? hVal1 + 0.360 : hVal1;
            var negativeHSL = new Vector4((float)hVal1, MathHelper.Clamp01(hsl.Y + random.Next(-20, 0) / 100f), MathHelper.Clamp01(hsl.Z + random.Next(-15, 15) / 100f), hsl.W);

            var c0 = Color.FromHsl(positiveHSL);
            var c1 = Color.FromHsl(negativeHSL);

            var palette = new List<Color> { color, c0, c1 };
            return palette;
        }

        // return a list of triadic complementary colors
        List<Color> GetTriadicComplementaries(Color color)
        {
            return GetComplementaryColors(color, 0.120);
        }

        // return a list of split complementary colors
        List<Color> GetSplitComplementaries(Color color)
        {
            return GetComplementaryColors(color, 0.150);
        }

        // return a list containing the base color and two copies of a single complemenary color
        List<Color> GetOneComplementary(Color color)
        {
            return GetComplementaryColors(color, 0.180);
        }

        Color SquashToSkinLuminosity(Color skinColor, Color toSquash)
        {
            var skinColorHSL = Color.ToHsl(skinColor);
            var toSquashHSL = Color.ToHsl(toSquash);

            // check if the skin color is as dark as or darker than the marking color:
            if (toSquashHSL.Z <= skinColorHSL.Z)
            {
                // if it is, don't fuck with it
                return toSquash;
            }

            // otherwise, create a new color with the H, S, and A of toSquash, but the L of skinColor
            var newColor = new Vector4(toSquashHSL.X, toSquashHSL.Y, skinColorHSL.Z, toSquashHSL.W);
            return Color.FromHsl(newColor);
        }
        // IMP EDIT END
        // SORRY TO ANYONE WHO HAS TO EDIT THIS
    }

    public static Color ClampColor(Color color)
    {
        return new(color.RByte, color.GByte, color.BByte);
    }

    public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance, ProtoId<SpeciesPrototype> species, Sex sex)
    {
        var eyeColor = ClampColor(appearance.EyeColor);

        var proto = IoCManager.Resolve<IPrototypeManager>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        var skinColor = appearance.SkinColor;
        var validatedMarkings = appearance.Markings.ShallowClone();

        if (proto.TryIndex(species, out var speciesProto))
        {
            var strategy = proto.Index(speciesProto.SkinColoration).Strategy;
            var organs = markingManager.GetOrgans(species);
            skinColor = strategy.EnsureVerified(skinColor);

            foreach (var (organ, markings) in appearance.Markings)
            {
                if (!organs.ContainsKey(organ))
                    validatedMarkings.Remove(organ);
            }

            foreach (var (organ, organProtoID) in organs)
            {
                if (!markingManager.TryGetMarkingData(organProtoID, out var organData))
                {
                    validatedMarkings.Remove(organ);
                    continue;
                }

                var actualMarkings = appearance.Markings.GetValueOrDefault(organ)?.ShallowClone() ?? [];

                markingManager.EnsureValidColors(actualMarkings);
                markingManager.EnsureValidGroupAndSex(actualMarkings, organData.Value.Group, sex);
                markingManager.EnsureValidLayers(actualMarkings, organData.Value.Layers);
                markingManager.EnsureValidLimits(actualMarkings, organData.Value.Group, organData.Value.Layers, skinColor, eyeColor);

                validatedMarkings[organ] = actualMarkings;
            }
        }

        return new HumanoidCharacterAppearance(
            eyeColor,
            skinColor,
            validatedMarkings);
    }

    public bool MemberwiseEquals(ICharacterAppearance maybeOther)
    {
        if (maybeOther is not HumanoidCharacterAppearance other) return false;
        if (!EyeColor.Equals(other.EyeColor)) return false;
        if (!SkinColor.Equals(other.SkinColor)) return false;
        if (!MarkingManager.MarkingsAreEqual(Markings, other.Markings)) return false;
        return true;
    }

    public bool Equals(HumanoidCharacterAppearance? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EyeColor.Equals(other.EyeColor) &&
               SkinColor.Equals(other.SkinColor) &&
               MarkingManager.MarkingsAreEqual(Markings, other.Markings);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EyeColor, SkinColor, Markings);
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
