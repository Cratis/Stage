// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Scriban.Runtime;

namespace Cratis.Stage.Rendering.Templates;

internal static class LanguageHelpers
{
    internal static void Install(ScriptObject root, ITemplateLanguage language, int stringLimit)
    {
        string Guard(string text)
        {
            if (text.Length > stringLimit) throw PlanMeter.Exceeded(TemplateCodes.StringLength, "Language helper string limit exceeded.");
            if (TemplatePreScan.HasPrivateUse(text)) throw JsonScriptValues.Invalid("Language helpers cannot emit private-use text.");
            return text;
        }

        var names = new ScriptObject();
        names.Import("words", new Func<string, ScriptArray>(TemplateNames.Words));
        foreach (var style in new[] { "pascal", "camel", "snake", "kebab", "constant" }) names.Import(style, new Func<string, string>(text => Guard(TemplateNames.Apply(style, text))));
        Set(root, "name", names);
        var literals = new ScriptObject();
        literals.Import("string", new Func<string, string>(value => Guard(language.EscapeString(value))));
        literals.Import("integer", new Func<object?, string>(value => Guard(language.FormatInteger(value is int or long ? Convert.ToInt64(value, CultureInfo.InvariantCulture) : throw JsonScriptValues.Invalid("lit.integer requires an integer.")))));
        literals.Import("decimal", new Func<object?, string>(value => Guard(language.FormatDecimal(value is int or long or decimal ? Convert.ToDecimal(value, CultureInfo.InvariantCulture) : throw JsonScriptValues.Invalid("lit.decimal requires a number.")))));
        literals.Import("boolean", new Func<object?, string>(value => Guard(language.FormatBoolean(value is bool boolean ? boolean : throw JsonScriptValues.Invalid("lit.boolean requires a Boolean.")))));
        literals.Import("null", new Func<string>(() => Guard(language.FormatNull())));
        literals.Import("comment", new Func<string, string>(value => Guard(language.EscapeComment(value))));
        literals.Import("identifier", new Func<string, string>(value => Guard(language.EscapeIdentifier(value))));
        Set(root, "lit", literals);
        var symbols = (ScriptObject)root["sym"]!;
        symbols.IsReadOnly = false;
        symbols.Import("decl", new Func<object?, string>(value =>
        {
            var symbol = SymbolHandle.Parse(value);
            return Guard(language.EscapeIdentifier(symbol.Name ?? symbol.Identity));
        }));
        symbols.Import("imports", new Func<object?, string>(value => Guard(language.FormatImports(ParseSymbols(value)))));
        symbols.IsReadOnly = true;
    }

    internal static IReadOnlyList<SymbolDescriptor> ParseSymbols(object? value)
    {
        if (value is not ScriptArray array) throw JsonScriptValues.Invalid("sym.imports requires an array of symbol descriptors.");
        return [.. array.Select(SymbolHandle.Parse).Distinct().OrderBy(symbol => symbol.Identity, StringComparer.Ordinal).ThenBy(symbol => symbol.Namespace, StringComparer.Ordinal).ThenBy(symbol => symbol.Name, StringComparer.Ordinal)];
    }

    internal static void Set(ScriptObject root, string name, ScriptObject value)
    {
        value.IsReadOnly = true;
        root.SetValue(name, value, true);
    }
}
