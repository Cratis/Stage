// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan.given;

public class a_java_plan : Specification
{
    internal static DeclarativeSymbolSegment Imports() => new(TemplateSymbolKind.Imports, null);

    internal static DeclarativeSymbolSegment Declaration(string name, string package = "example") => new(TemplateSymbolKind.Declaration, new(package, name));

    internal static DeclarativeSymbolSegment Reference(string name, string package = "example") => new(TemplateSymbolKind.Reference, new(package, name));

    internal static DeclarativeLanguageFile File(string path, params DeclarativeSegment[] segments) => new(path, [.. segments]);

    internal static DeclarativeLanguageFile Java(string path, string package, params string[] declarations) =>
        File(path, [Imports(), .. declarations.Select(name => Declaration(name, package))]);

    internal static string Name(JavaPlan plan, string original, string package = "example") => plan.Declarations[new(package, original)].Allocated.Name;

    internal static string Suffix(string name, int length = 8, string package = "example") => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(package + "\n" + name)))[..length];

    internal static ImmutableArray<string> Codes(params DeclarativeLanguageFile[] files) =>
        [((LanguagePackRefusal)Catch.Exception(() => JavaPlan.Create([.. files]))).Code];
}
