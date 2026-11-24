// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

internal static class round_trip_model_profile
{
    // V48 is the lowest supported pair covering the eligible corpus without a must-use rule; its agents are optional.
    public static (LanguageVersion Language, SemanticVersion Semantic) Common { get; } = (LanguageVersion.V48, SemanticVersion.V48);

    public static bool Covers(ExecutableSemanticModel model) => AtLeast(Common.Language, model.LanguageVersion) && Common.Semantic.IsAtLeast(model.SemanticVersion);

    public static void EnsureCovered(ExecutableSemanticModel model)
    {
        if (!Covers(model)) throw new InvalidSemanticContract($"The Arc round-trip comparison profile {Common} cannot downgrade a model declared as ({model.LanguageVersion}, {model.SemanticVersion}).");
    }

    public static ExecutableSemanticModel Convert(ExecutableSemanticModel model)
    {
        // Validate under the declared source contract before any migration; never repair an invalid source graph.
        var validated = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application);
        var profile = Common;
        EsmSchemaV48Support.EnsureSupported(profile.Language, profile.Semantic);
        EnsureCovered(validated);
        if (model.LanguageVersion == profile.Language && model.SemanticVersion == profile.Semantic) return validated;

        var application = round_trip_command_destinations.Promote(validated.Application);
        if (!model.SemanticVersion.IsAtLeast(SemanticVersion.V37))
        {
            // The pinned writer's effective historical calendar is nonconfigurable UTC, not a deleted comparison field.
            application = application with { TimeZone = application.TimeZone ?? new SemanticTimeZone("UTC", false) };
        }

        // The pinned canonical writer supplies version-gated empty/default fields; this recomputes the revision and validates the target graph.
        return ExecutableSemanticModel.Create(profile.Language, profile.Semantic, application);
    }

    static bool AtLeast(LanguageVersion left, LanguageVersion right) => left.Major > right.Major || (left.Major == right.Major && left.Minor >= right.Minor);
}
#endif
