// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        foreach (var style in new[] { "words", "pascal", "camel", "snake", "kebab", "constant" }) names.Import(style, new Func<object?, string>(value => Guard(TemplateNames.Apply(style, value))));
        Set(root, "name", names);
        var literals = new ScriptObject();
        literals.Import("string", new Func<object?, string>(value => Guard(language.String(Text(value)))));
        literals.Import("integer", new Func<object?, string>(value => Guard(language.Integer(value switch { long integer => integer, int integer => integer, _ => throw JsonScriptValues.Invalid("lit.integer requires a long.") }))));
        literals.Import("decimal", new Func<object?, string>(value => Guard(language.Decimal(value switch { long integer => integer, int integer => integer, decimal number => number, _ => throw JsonScriptValues.Invalid("lit.decimal requires a number.") }))));
        literals.Import("boolean", new Func<object?, string>(value => Guard(language.Boolean(value is bool boolean ? boolean : throw JsonScriptValues.Invalid("lit.boolean requires a Boolean.")))));
        literals.Import("null", new Func<string>(() => Guard(language.Null())));
        literals.Import("comment", new Func<object?, string>(value => Guard(language.Comment(Text(value)))));
        literals.Import("identifier", new Func<object?, string>(value => Guard(language.Identifier(Text(value)))));
        Set(root, "lit", literals);
    }

    internal static string Text(object? value) => value as string ?? throw JsonScriptValues.Invalid("Expected text.");

    internal static void Set(ScriptObject root, string name, ScriptObject value)
    {
        value.IsReadOnly = true;
        root.SetValue(name, value, true);
    }
}
