// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Formats deterministic single-type imports; wildcard and static imports are never emitted.
/// </summary>
public static class JavaImportBlock
{
    /// <summary>
    /// Renders distinct, ordinally sorted imports, each followed by the chosen line ending.
    /// </summary>
    /// <param name="types">The structured types selected by a file scope.</param>
    /// <param name="newline">The output line ending.</param>
    /// <returns>The import block, or an empty string if no imports are needed.</returns>
    /// <exception cref="LanguagePackRefusal">An import is unnamed or contains an invalid Java identifier.</exception>
    public static string Render(IEnumerable<TypeName> types, string newline = "\n")
    {
        var imports = types.ToArray();
        foreach (var type in imports)
        {
            if (type.Package.Length == 0 || !type.Package.Split('.').Concat([type.Name]).Concat(type.Nested).All(JavaIdentifiers.IsIdentifier))
            {
                throw new LanguagePackRefusal(JavaRefusalCodes.InvalidIdentifier, $"Invalid Java single-type import '{type.QualifiedName}'.");
            }
        }

        return string.Concat(imports.Select(type => type.QualifiedName).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
            .Select(name => "import " + name + ";" + newline));
    }
}
