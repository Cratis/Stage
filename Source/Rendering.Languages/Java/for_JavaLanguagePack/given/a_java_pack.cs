// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Declarative;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.given;

public class a_java_pack : for_JavaPlan.given.a_java_plan
{
    const string FactsJson = """
        {"types":{
          "lib.Thing":[],"other.Thing":[],"lib.Base":["String"],"lib.Plain":[],"q.Item":[],"com.x.Y":[],"org.Y":[],
          "lib.Order":[],"lib.Hidden":[],"lib.Evil":[],"lib.Outer":[],"lib.Shadowing":["Outer"],"lib.Fields":["com"],
          "lib.Javaish":["java"],"Item.z.Y":[]
        }}
        """;

    protected JavaLanguagePack _pack = new(JavaTypeFacts.Parse(FactsJson));

    internal static DeclarativeTextSegment Txt(string value) => new(value);

    internal static DeclarativeLanguageFile Class(string package, string name, params DeclarativeSegment[] body) =>
        File(name + ".java", [Imports(), Txt("public class "), Declaration(name, package), Txt(" {\n"), .. body, Txt("}\n")]);

    internal static DeclarativeLanguageFile Nested(string name, params DeclarativeSegment[] body) =>
        File(name + ".java", [Imports(), Txt("public class "), Declaration(name), Txt(" {\n  static class "), Declaration(name + ".Inner"), Txt(" {\n"), .. body, Txt("  }\n}\n")]);

    internal ImmutableArray<DeclarativeFileResolution> Resolve(params DeclarativeLanguageFile[] files) => _pack.Resolve([.. files]);

    internal static string Text(DeclarativeFileResolution resolution) => string.Concat(resolution.Pieces.Select(piece => piece switch
    {
        DeclarativeGeneratedPiece generated => generated.Text,
        DeclarativeAuthoredPiece authored => authored.Text,
        _ => string.Empty
    }));

    internal static string Code(DeclarativeFileResolution resolution) => resolution.Refusal?.Code ?? string.Empty;

    internal static string Header(params string[] imports) =>
        "package example;\n\n" + string.Concat(imports.Select(import => "import " + import + ";\n")) + (imports.Length > 0 ? "\n" : string.Empty);

    internal static string Expected(string name, string body, params string[] imports) => Header(imports) + "public class " + name + " {\n" + body + "}\n";
}
