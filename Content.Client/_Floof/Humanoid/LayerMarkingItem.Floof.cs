// File is an extension of LayerMarkingItem.xaml.cs
using Content.Client.Guidebook.Controls;
using Content.Shared.Humanoid.Markings;
using Robust.Client.UserInterface.Controls;
using Content.Shared._Floof.Sprite;

namespace Content.Client.Humanoid;

public sealed partial class LayerMarkingItem : BoxContainer, ISearchableControl
{
    /// <summary>
    ///     Checks if the specified sprite at the index of a MarkingPrototype contains any ColorLinks.
    /// </summary>
    /// <param name="marking">The marking prototype to check against</param>
    /// <param name="spriteIndex">Index of the sprite we're checking</param>
    private bool HasColorLinks(MarkingPrototype marking, int spriteIndex)
    {
        if (marking.ColorLinks?.Count > 0)
        {
            var name = marking.Sprites[spriteIndex].GetFilename();
            if (marking.ColorLinks.ContainsKey(name))
                return true;
        }
        return false;
    }
}
