// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

/// <summary>Synthetic binding cases whose complete resolved Java is pinned as goldens.</summary>
public static class K1GoldenCases
{
    const string Header = "// Copyright (c) Cratis. All rights reserved.\n// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\n";

    internal const string FactsJson = """{"types":{"library.Base":["String"],"library.Outer":[],"com.library.Outer":[]}}""";

    /// <summary>Gets the test facts the cases rely on.</summary>
    public static JavaTypeFacts Facts() => JavaTypeFacts.Parse(FactsJson);

    /// <summary>Gets the synthetic files.</summary>
    /// <returns>The files.</returns>
    public static ImmutableArray<DeclarativeLanguageFile> Files() =>
    [
        File("collision", "vectors.collision", "Collision", [
            Text(" {\n  static class "), Declaration("vectors.collision", "Collision.Order"), Text(" {}\n  static class "), Declaration("vectors.collision", "Collision.order"), Text(" {}\n  "),
            Reference("vectors.collision", "Collision.Order"), Text(" upper;\n  "), Reference("vectors.collision", "Collision.order"), Text(" lower;\n")]),
        File("inheritance", "vectors.inheritance", "Child", [
            Text(" extends "), Reference("library", "Base"), Text(" {\n  "), Reference("java.lang", "String"), Text(" value;\n")]),
        File("nesting", "vectors.nesting", "Outer", [
            Text(" {\n  static class "), Declaration("vectors.nesting", "Outer.Inner"), Text(" {\n    "), Reference("library", "Outer"), Text(" foreign;\n  }\n  "), Reference("vectors.nesting", "Outer.Inner"), Text(" inner;\n")]),
        File("obscured", "vectors.obscured", "Child", [
            Text(" {\n  int com;\n  "), Reference("com.library", "Outer"), Text(" value;\n")]),
        File("unknown", "vectors.unknown", "Child", [
            Text(" {\n  "), Reference("vectors.unknown", "Missing"), Text(" value;\n")]),
    ];

    static DeclarativeTextSegment Text(string value) => new(value);

    static DeclarativeSymbolSegment Declaration(string package, string name) => new(TemplateSymbolKind.Declaration, new(package, name));

    static DeclarativeSymbolSegment Reference(string package, string name) => new(TemplateSymbolKind.Reference, new(package, name));

    static DeclarativeLanguageFile File(string id, string package, string name, DeclarativeSegment[] body) =>
        new(package.Replace('.', '/') + "/" + name + ".java", [Text(Header), new DeclarativeSymbolSegment(TemplateSymbolKind.Imports, null), Text("public class "), Declaration(package, name), .. body, Text("}\n")]);
}
