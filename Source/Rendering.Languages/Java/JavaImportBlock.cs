// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Formats the package declaration and deterministic single-type imports; wildcard and static imports are never emitted.
/// </summary>
public static class JavaImportBlock
{
    /// <summary>
    /// Renders the package declaration followed by distinct, ordinally sorted imports. A <c language="java">java.lang</c> import is an ordinary entry.
    /// </summary>
    /// <param name="package">The dotted package of the file.</param>
    /// <param name="importedTypes">The canonical dotted names of the types to import.</param>
    /// <returns>The block <c language="java">package p;</c>, a blank line, each import line and, when any import exists, a trailing blank line.</returns>
    /// <exception cref="LanguagePackRefusal">The package is empty, or a name is not a valid single-type import.</exception>
    public static string Render(string package, IEnumerable<string> importedTypes)
    {
        if (package.Length == 0)
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.DefaultPackage, "A Java file needs a named package.");
        }

        ValidatePackage(package);
        var imports = importedTypes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        foreach (var import in imports)
        {
            var segments = import.Split('.');
            if (segments.Length < 2 || !segments.All(JavaIdentifiers.IsIdentifier))
            {
                throw new LanguagePackRefusal(JavaRefusalCodes.InvalidIdentifier, $"Invalid Java single-type import '{import}'.");
            }
        }

        return "package " + package + ";\n\n" + string.Concat(imports.Select(import => "import " + import + ";\n")) + (imports.Length > 0 ? "\n" : string.Empty);
    }

    static void ValidatePackage(string package)
    {
        if (!package.Split('.').All(JavaIdentifiers.IsIdentifier))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.InvalidIdentifier, $"Invalid Java package '{package}'.");
        }
    }
}
