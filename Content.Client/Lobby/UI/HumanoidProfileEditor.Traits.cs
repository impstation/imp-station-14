using System.Linq;
using Content.Client.Lobby.UI.Roles;
using Content.Client.Stylesheets;
using Content.Shared.Traits;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Content.Shared._Impstation.Traits; // Imp add
using Robust.Shared.Prototypes; // Imp add

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{

    /// <summary>
    /// Refreshes traits selector
    /// </summary>
    public void RefreshTraits()
    {
        TraitsList.RemoveAllChildren();

        // imp edit -- sort trait points by their cost, then their name
        // var traits = _prototypeManager.EnumeratePrototypes<TraitPrototype>().OrderBy(t => Loc.GetString(t.Name)).ToList();
        var traits = _prototypeManager.EnumeratePrototypes<TraitPrototype>().OrderByDescending(t => t.Cost).ThenBy(t => Loc.GetString(t.Name)).ToList();
        // imp edit end
        TabContainer.SetTabTitle(3, Loc.GetString("humanoid-profile-editor-traits-tab"));

        if (traits.Count < 1)
        {
            TraitsList.AddChild(new Label
            {
                Text = Loc.GetString("humanoid-profile-editor-no-traits"),
                FontColorOverride = Color.Gray,
            });
            return;
        }

        // Setup model
        Dictionary<string, List<string>> traitGroups = new();
        List<string> defaultTraits = new();
        traitGroups.Add(TraitCategoryPrototype.Default, defaultTraits);

        foreach (var trait in traits)
        {
            // Begin DeltaV Additions - Species trait exlusion
            if (Profile?.Species is { } selectedSpecies && trait.ExcludedSpecies.Contains(selectedSpecies))
            {
                Profile = Profile?.WithoutTraitPreference(trait.ID, _prototypeManager);
                continue;
            }
            // End DeltaV Additions

            if (trait.Category == null)
            {
                defaultTraits.Add(trait.ID);
                continue;
            }

            if (!_prototypeManager.HasIndex(trait.Category))
                continue;

            var group = traitGroups.GetOrNew(trait.Category);
            group.Add(trait.ID);
        }

        // Create UI view from model
        foreach (var (categoryId, categoryTraits) in traitGroups)
        {
            TraitCategoryPrototype? category = null;

            if (categoryId != TraitCategoryPrototype.Default)
            {
                category = _prototypeManager.Index<TraitCategoryPrototype>(categoryId);
                // Label
                TraitsList.AddChild(new Label
                {
                    Text = Loc.GetString(category.Name),
                    Margin = new Thickness(0, 10, 0, 0),
                    StyleClasses = { StyleClass.LabelHeading },
                });
            }

            List<TraitPreferenceSelector?> selectors = new();
            List<ProtoId<TraitSubcategoryPrototype>> usedSubcategories = []; // imp addition
            var selectionCount = 0;

            foreach (var traitProto in categoryTraits)
            {
                var trait = _prototypeManager.Index<TraitPrototype>(traitProto);
                var selector = new TraitPreferenceSelector(trait);

                selector.Preference = Profile?.TraitPreferences.Contains(trait.ID) == true;
                if (selector.Preference)
                // begin Imp edits - ignore any traits that conflict with subcategories of already selected traits
                {
                    selectionCount += trait.Cost;
                    foreach (var subcategory in trait.Subcategories)
                    {
                        if (!usedSubcategories.Contains(subcategory))
                        {
                            usedSubcategories.Add(subcategory);
                        }
                        else
                        {
                            Profile = Profile?.WithoutTraitPreference(trait.ID, _prototypeManager);
                        }
                    }
                }
                // end imp edits

                selector.PreferenceChanged += preference =>
                {
                    if (preference)
                    {
                        Profile = Profile?.WithTraitPreference(trait.ID, _prototypeManager);
                    }
                    else
                    {
                        Profile = Profile?.WithoutTraitPreference(trait.ID, _prototypeManager);
                    }

                    SetDirty();
                    RefreshTraits(); // If too many traits are selected, they will be reset to the real value.
                };
                selectors.Add(selector);
            }

            // Selection counter
            if (category is { MaxTraitPoints: >= 0 })
            {
                if (category.MaxTraitPoints != null) //imp - extremely stupid null check because rider isn't happy about the potential of subtracting from a nullable int
                {
                    int maxPoints = (int)category.MaxTraitPoints; //imp - cast to a non-nullable int
                    TraitsList.AddChild(new Label
                    {
                        Text = Loc.GetString("humanoid-profile-editor-trait-count-hint", ("current", maxPoints - selectionCount), ("max", category.MaxTraitPoints)), // imp edit -- count points backwards
                        FontColorOverride = Color.Gray
                    });
                }
            }

            // imp addition start:
            // instead of appending everything to traitslist, we make a new boxcontainer and put the selectors in there.
            var traitColumn1 = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Margin = new Thickness(10, 0, 10, 0)
            };
            var traitColumn2 = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            var traitBoxContainer = new BoxContainer { };
            traitBoxContainer.AddChild(traitColumn1);
            traitBoxContainer.AddChild(traitColumn2);

            var i = 0;
            // imp end

            foreach (var selector in selectors)
            {
                if (selector == null)
                    continue;

                if (category is { MaxTraitPoints: >= 0 } &&
                    selector.Cost + selectionCount > category.MaxTraitPoints)
                {
                    selector.Checkbox.Label.FontColorOverride = Color.Red;
                }

                TraitsList.AddChild(selector);

                // begin Imp additions -- disallow players from selecting multiple traits in the same subcategory
                if (!selector.Preference && selector.Subcategories.Overlaps(usedSubcategories))
                {
                    selector.Checkbox.Label.FontColorOverride = Color.Red;
                }
                // end Imp additions

                // imp start: ui layout
                if (i == 0)
                {
                    traitColumn1.AddChild(selector);
                    i += 1;
                }
                else
                {
                    traitColumn2.AddChild(selector);
                    i -= 1;
                }
            }
            TraitsList.AddChild(traitBoxContainer);
            // imp end
        }
    }
}
