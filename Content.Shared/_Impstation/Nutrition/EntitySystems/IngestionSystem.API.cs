using System.Diagnostics.CodeAnalysis;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// Extension of the base-directory ingestion API.
/// </summary>
public sealed partial class IngestionSystem
{

    /// <inheritdoc cref="TryIngest(EntityUid,EntityUid)"/>
    /// <summary>Overload of TryIngest for if an entity is trying to make another entity ingest an entity</summary>
    /// <param name="user">The entity who is trying to make this happen.</param>
    /// <param name="target">The entity who is being made to ingest something.</param>
    /// <param name="ingested">The entity that is trying to be ingested.</param>
    public bool TryRepeatIngest(EntityUid user, EntityUid target, EntityUid ingested)
    {
        return AttemptIngest(user, target, ingested, true, tryRepeat: true);
    }
    #region Edible Types

    /// <summary>
    /// Tries to get the repeat ingestion verbs for a given user entity and ingestible entity.
    /// </summary>
    /// <param name="user">The one getting the verbs who would be doing the eating.</param>
    /// <param name="ingested">Entity being ingested.</param>
    /// <param name="type">Edible prototype.</param>
    /// <param name="verb">Verb we're returning.</param>
    /// <returns>Returns true if we generated a verb.</returns>
    public bool TryGetRepeatIngestionVerb(EntityUid user, EntityUid ingested, [ForbidLiteral] ProtoId<EdiblePrototype> type, [NotNullWhen(true)] out AlternativeVerb? verb)
    {
        verb = null;

        // We want to see if we can ingest this item, but we don't actually want to ingest it.
        if (!CanIngest(user, ingested))
            return false;

        var proto = _proto.Index(type);

        // Check if the repeat verb is enabled and it's verb is set in the edible type.
        if (!proto.AllowRepeatIngestion || proto.RepeatVerbName == null)
            return false;

        verb = new()
        {
            Act = () =>
            {
                TryRepeatIngest(user, user, ingested);
            },
            Icon = proto.VerbIcon,
            Text = Loc.GetString(proto.RepeatVerbName),
            Priority = 1
        };

        return true;
    }
    #endregion
}
