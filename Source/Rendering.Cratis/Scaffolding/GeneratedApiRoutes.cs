// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Shares the scaffold's route settings with explicit paths for escaped query methods.
/// </summary>
internal static class GeneratedApiRoutes
{
    internal const string RoutePrefix = "api";
    internal const int SegmentsToSkipForRoute = 1;

    internal static string QueryPath(string @namespace, string originalMethod) =>
        "/" + string.Join('/', new[] { RoutePrefix }.Concat(@namespace.Split('.').Skip(SegmentsToSkipForRoute)
            .Append(originalMethod).Select(KebabCase)));

    // Match Arc's endpoint spelling, including a dash before each acronym letter and for underscores.
    static string KebabCase(string name)
    {
        var result = new StringBuilder();
        for (var index = 0; index < name.Length; index++)
        {
            var character = name[index];
            if (character == '_')
            {
                result.Append('-');
                continue;
            }
            if (char.IsUpper(character) && index > 0 && name[index - 1] != '_') result.Append('-');
            result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }
}
