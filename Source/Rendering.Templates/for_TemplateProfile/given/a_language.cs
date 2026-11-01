// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Stage.Rendering.Kits;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile.given;

internal sealed class a_language : ITemplateLanguage, ITypeDirectory
{
    public LanguagePackVersion Pack => new("test", "1.0.0");

    public string String(string value) => JsonSerializer.Serialize(value);

    public string Integer(long value) => value.ToString(CultureInfo.InvariantCulture);

    public string Decimal(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    public string Boolean(bool value) => value ? "yes" : "no";

    public string Null() => "nil";

    public string Comment(string value) => value.Replace("*/", "* /", StringComparison.Ordinal).Replace('\n', ' ');

    public string Identifier(string value) => "@" + value;

    public TypeSpelling Primitive(string primitive) => primitive switch
    {
        "text" => new([new TemplateTextSegment("string")]),
        "wholeNumber" => new([new TemplateTextSegment("long")]),
        "uuid" => new([new TemplateSymbolSegment(TemplateSymbolKind.Reference, new("System", "Guid"))]),
        _ => throw new TemplateLanguageRefusal("Unknown primitive.")
    };

    public TypeSpelling Collection(TypeSpelling element) => new([.. element.Parts, new TemplateTextSegment("[]")]);

    public TypeSpelling Optional(TypeSpelling element) => new([.. element.Parts, new TemplateTextSegment("?")]);

    public string Placeholder(GapRole role, string code, string intent) => "FAIL[" + role + ":" + code + "]";

    public bool TryGet(string semanticId, out TemplateSymbol symbol)
    {
        symbol = new(semanticId == "other" ? "Other" : "Domain", "Customer");
        return semanticId == "customer" || semanticId == "other";
    }
}
