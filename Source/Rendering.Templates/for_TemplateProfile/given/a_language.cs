// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile.given;

internal sealed class a_language(string id = "stub") : ITemplateLanguage
{
    public string LanguageId => id;

    public string EscapeString(string value) => JsonSerializer.Serialize(value);

    public string FormatInteger(long value) => value.ToString(CultureInfo.InvariantCulture);

    public string FormatDecimal(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    public string FormatBoolean(bool value) => value ? "yes" : "no";

    public string FormatNull() => "nil";

    public string EscapeComment(string value) => value.Replace("*/", "* /", StringComparison.Ordinal).Replace('\n', ' ');

    public string EscapeIdentifier(string value) => "@" + value;

    public TemplateSegment[]? RenderType(TemplateType type) => type.Kind == "unsupported" ? null : [new TemplateTextSegment(id + ":" + type.Kind)];

    public string FormatImports(IReadOnlyList<SymbolDescriptor> symbols) => string.Join(',', symbols.Select(symbol => symbol.Identity));

    public TemplateSegment[] EmitGap(TemplateGap gap) => [new TemplateTextSegment("FAIL[" + gap.Role + ":" + gap.Code + "]")];
}
