// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using System.Reflection;
using Cratis.Screenplay.Conformance;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

internal static class round_trip_shared_version
{
    // The pinned writer is used only at the unchanged shared pair when its introduction requires unavailable content.
    static readonly Func<LanguageVersion, SemanticVersion, SemanticApplication, byte[]> HistoricalJson =
        typeof(SemanticModelCanonicalJson).GetMethod(
            "SerializeWithoutRevision",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(LanguageVersion), typeof(SemanticVersion), typeof(SemanticApplication)],
            null)!.CreateDelegate<Func<LanguageVersion, SemanticVersion, SemanticApplication, byte[]>>();

    public static NormalizedSemanticModel Normalize(ExecutableSemanticModel model)
    {
        try
        {
            var validated = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application);
            return SemanticModelNormalizer.Normalize(validated);
        }
        catch (InvalidSemanticContract exception) when (RequiresUnavailableIntroduction(exception, model.SemanticVersion))
        {
            return SemanticModelNormalizer.NormalizeCanonicalJson(HistoricalJson(model.LanguageVersion, model.SemanticVersion, model.Application));
        }
    }

    static bool RequiresUnavailableIntroduction(InvalidSemanticContract exception, SemanticVersion version) =>
        exception.Message.StartsWith($"An ESM v{version.Major.ToString(CultureInfo.InvariantCulture)} model must ", StringComparison.Ordinal);
}
#endif
