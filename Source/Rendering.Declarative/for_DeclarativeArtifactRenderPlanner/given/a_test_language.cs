// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Cratis.Stage.Rendering.Kits;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.given;

/// <summary>
/// The spec-only <c lang="text">test@1.0.0</c> language: C#-like literals, <see langword="using"/> imports and short names only.
/// </summary>
/// <param name="version">The pack version it answers to.</param>
/// <param name="id">The pack identifier it answers to.</param>
internal sealed class a_test_language(string version = "1.0.0", string id = "test") : IDeclarativeLanguage
{
    public const string AmbiguousName = "TEST-001";
    public const string UnsupportedBody = "TEST-002";
    public const string RefusedBody = "TEST-003";

    public LanguagePackVersion Pack => new(id, version);

    public string String(string value) => JsonSerializer.Serialize(value);

    public string Integer(long value) => value.ToString(CultureInfo.InvariantCulture);

    public string Decimal(decimal value) => value.ToString(CultureInfo.InvariantCulture) + "m";

    public string Boolean(bool value) => value ? "true" : "false";

    public string Null() => "null";

    public string Comment(string value) => "// " + value.Replace('\n', ' ');

    public string Identifier(string value) => value;

    public TypeSpelling Primitive(string primitive) => primitive switch
    {
        "text" => new([new TemplateTextSegment("string")]),
        "wholeNumber" => new([new TemplateTextSegment("long")]),
        "uuid" => new([new TemplateSymbolSegment(TemplateSymbolKind.Reference, new("System", "Guid"))]),
        _ => throw new TemplateLanguageRefusal("Unknown primitive.")
    };

    public TypeSpelling Collection(TypeSpelling element) => new([.. element.Parts, new TemplateTextSegment("[]")]);

    public TypeSpelling Optional(TypeSpelling element) => new([.. element.Parts, new TemplateTextSegment("?")]);

    public string Placeholder(GapRole role, string code, string intent) => $"/* {role} {code} */";

    public bool TryGet(string semanticId, out TemplateSymbol symbol)
    {
        symbol = new("Domain", "Item");
        return false;
    }

    public DeclarativeFileResolution Resolve(string path, ImmutableArray<DeclarativeSegment> segments)
    {
        var symbols = segments.OfType<DeclarativeSymbolSegment>().Where(_ => _.Value is not null).Select(_ => _.Value!).Distinct().ToArray();
        var clash = symbols.GroupBy(_ => _.Name, StringComparer.Ordinal).FirstOrDefault(_ => _.Count() > 1);
        if (clash is not null)
        {
            return DeclarativeFileResolution.Refused(AmbiguousName, $"The short name {clash.Key} is ambiguous in {path}.");
        }

        var home = segments.OfType<DeclarativeSymbolSegment>().FirstOrDefault(_ => _.Kind == TemplateSymbolKind.Declaration)?.Value?.Namespace;
        var imports = string.Concat(symbols.Select(_ => _.Namespace).Where(_ => _ != home).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Select(_ => $"using {_};\n"));
        return DeclarativeFileResolution.Resolved([.. segments.Select(Piece(imports))]);
    }

    public AuthoredAdmission Admit(string requirementId, string body)
    {
        if (body.Contains("refuse", StringComparison.Ordinal))
        {
            return AuthoredAdmission.Refused(RefusedBody, "The body fails a trust check.");
        }

        return body.Contains("forbidden", StringComparison.Ordinal) || CarriageReturnInLiteral(body)
            ? AuthoredAdmission.Unsupported(UnsupportedBody, "The body uses an unsupported construct.")
            : AuthoredAdmission.Admitted;
    }

    static bool CarriageReturnInLiteral(string body) => body.Split('"').Where((_, index) => index % 2 == 1).Any(_ => _.Contains((char)13));

    static Func<DeclarativeSegment, DeclarativePiece> Piece(string imports) => segment => segment switch
    {
        DeclarativeTextSegment text => new DeclarativeGeneratedPiece(text.Value),
        DeclarativeSymbolSegment { Kind: TemplateSymbolKind.Imports } => new DeclarativeGeneratedPiece(imports),
        DeclarativeSymbolSegment symbol => new DeclarativeGeneratedPiece(symbol.Value!.Name),
        DeclarativeAuthoredSegment authored => new DeclarativeAuthoredPiece(authored.Body),
        _ => throw new InvalidOperationException("Unknown segment.")
    };
}
