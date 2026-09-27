// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// Shares generated CLR member spelling with admission checks compiled into the specification executor.
/// </summary>
internal static class GeneratedPascalCase
{
    internal static string From(string name)
    {
        var result = string.Concat(name.Split([' ', '_', '-', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        if (result.Length == 0)
        {
            return "Item";
        }

        return char.IsDigit(result[0]) ? $"_{result}" : result;
    }
}
