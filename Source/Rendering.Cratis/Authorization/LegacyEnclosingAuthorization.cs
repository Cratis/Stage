// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Authorization;

/// <summary>
/// Refuses to drop inherited authorization on the syntax-based rendering path.
/// </summary>
internal static class LegacyEnclosingAuthorization
{
    internal static void EnsureRenderable(LocatedSlice slice, ApplicationSet applications, string subject)
    {
        foreach (var module in applications.Applications.SelectMany(application => application.Modules))
        {
            if (Find(module.Features, slice.Slice, module.Authorize is not null))
            {
                throw new AuthorizationCannotBeRendered(subject, "inherits module or feature authorization that the legacy attribute renderer cannot conjoin with artifact authorization");
            }
        }
    }

    static bool Find(IEnumerable<FeatureSyntax> features, SliceSyntax slice, bool inherited)
    {
        foreach (var feature in features)
        {
            var protectedHere = inherited || feature.Authorize is not null;
            if (feature.Slices.Any(candidate => ReferenceEquals(candidate, slice)))
            {
                return protectedHere;
            }

            if (Find(feature.Features, slice, protectedHere))
            {
                return true;
            }
        }

        return false;
    }
}
