using System.Numerics;

namespace Content.Shared.Humanoid.Markings.ColoringTypes;

/// <summary>
///     Colors layer with a hue shifted and brightened skin color
/// </summary>
public sealed partial class HueShiftedColoring : LayerColoringType
{
    public override Color? GetCleanColor(Color? skin, Color? eye, List<Marking> otherMarkings)
    {
        if (skin == null)
        {
            return null;
        }

        var startHue = Color.ToHsv(skin.Value);

        var shiftHue = Math.Abs (0.5f % startHue.X); // shift hue 180 degrees on color wheel
        if (startHue.X < 0.3f || startHue.X == 0.0f) // ensure the color can be shifted
        {
            shiftHue = (0.2f + startHue.X);
        }

        startHue.Y = startHue.Y * 1.5f; //increase saturation

        startHue.Z = startHue.Z * 1.5f; //brighten it
        if (startHue.Z <= 0.3) // if it's too dark, raise the value even more
        {
            startHue.Z = startHue.Z * 2.2f;
        }

        var finalHue = shiftHue + startHue.X; // add all that stuff we just did together


        return Color.FromHsv(new Vector4(finalHue, startHue.Y, startHue.Z, startHue.Z));
    }
}
