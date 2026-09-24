// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Authorization;

/// <summary>
/// Refuses to drop inherited authorization on the syntax-based rendering path.
/// </summary>
/// <remarks>
/// The legacy attribute renderer cannot conjoin module or feature authorization with an artifact's own, so a slice
/// that inherits any fails closed. A slice that cannot be located in the application set fails closed too: its
/// enclosing authorization is unknown, and rendering it would assume there is none.
/// </remarks>
internal static class LegacyEnclosingAuthorization
{
    internal static void EnsureRenderable(LocatedSlice slice, ApplicationSet applications, string subject)
    {
        var modules = applications.Applications.SelectMany(application => application.Modules).ToArray();
        var inherited = modules.Select(module => Find(module.Features, slice.Slice, module.Authorize is not null)).FirstOrDefault(found => found is not null)
            ?? ByPath(modules, slice)
            ?? throw new AuthorizationCannotBeRendered(subject, "is not part of the rendered application set, so the authorization it inherits cannot be determined");
        if (inherited)
        {
            throw new AuthorizationCannotBeRendered(subject, "inherits module or feature authorization that the legacy attribute renderer cannot conjoin with artifact authorization");
        }
    }

    static bool? Find(IEnumerable<FeatureSyntax> features, SliceSyntax slice, bool inherited)
    {
        foreach (var feature in features)
        {
            var protectedHere = inherited || feature.Authorize is not null;
            if (feature.Slices.Any(candidate => ReferenceEquals(candidate, slice)))
            {
                return protectedHere;
            }

            if (Find(feature.Features, slice, protectedHere) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    // A slice rewritten as a copy is no longer the instance the application holds; its location still names it.
    static bool? ByPath(IEnumerable<ModuleSyntax> modules, LocatedSlice slice)
    {
        if (slice.Path.Count < 2)
        {
            return null;
        }

        foreach (var module in modules.Where(module => module.Name == slice.Path[0]))
        {
            var protectedHere = module.Authorize is not null;
            var features = module.Features.AsEnumerable();
            FeatureSyntax? feature = null;
            foreach (var name in slice.Path.Skip(1))
            {
                feature = features.FirstOrDefault(candidate => candidate.Name == name);
                if (feature is null)
                {
                    break;
                }

                protectedHere |= feature.Authorize is not null;
                features = feature.Features;
            }

            if (feature?.Slices.Any(candidate => candidate.Name == slice.Slice.Name) == true)
            {
                return protectedHere;
            }
        }

        return null;
    }
}
