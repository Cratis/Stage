// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Strings;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Carries validated, canonical locale dictionaries in the immutable render profile.
/// </summary>
internal static partial class StringsCatalogInput
{
    internal const string Name = "cratis-strings:catalog";
    internal const string Version = "1";
    internal const string RelativePath = "GeneratedStrings.cs";

    [GeneratedRegex("^[A-Za-z]{2,8}(?:-[A-Za-z0-9]{2,8})*$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex LocalePattern { get; }

    [GeneratedRegex(@"^[A-Za-z_]\w*(?:\.\w+)*$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex KeyPattern { get; }

    internal static ArtifactRenderInput Create(IReadOnlyDictionary<string, string> files, string defaultLocale)
    {
        if (!LocalePattern.IsMatch(defaultLocale) || files.Count == 0)
        {
            throw Invalid();
        }

        var locales = new SortedDictionary<string, SortedDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var (path, content) in files.OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            var file = Path.GetFileName(path);
            if (path.StartsWith('/') || path.Split('/').Any(_ => _.Length == 0 || _ == "." || _ == "..") ||
                path.Contains('\\') || path.Any(char.IsControl) || !file.EndsWith(".strings", StringComparison.Ordinal))
            {
                throw Invalid();
            }

            var stem = file[..^".strings".Length];
            var separator = stem.LastIndexOf('.');
            if (separator <= 0 || !LocalePattern.IsMatch(stem[(separator + 1)..]))
            {
                throw Invalid();
            }

            var locale = stem[(separator + 1)..];
            if (locales.Keys.Any(existing => string.Equals(existing, locale, StringComparison.OrdinalIgnoreCase) && existing != locale))
            {
                throw Invalid();
            }

            if (!locales.TryGetValue(locale, out var entries))
            {
                entries = new(StringComparer.Ordinal);
                locales.Add(locale, entries);
            }

            try
            {
                foreach (var entry in StringsFile.Parse(content).Entries)
                {
                    if (entry.Value.Any(character => char.IsControl(character) || char.IsSurrogate(character)) ||
                        !entries.TryAdd(entry.Key, entry.Value))
                    {
                        throw Invalid();
                    }
                }
            }
            catch (InvalidStringsLine)
            {
                throw Invalid();
            }
        }

        if (!locales.ContainsKey(defaultLocale))
        {
            throw Invalid();
        }

        return ArtifactRenderInput.Create(Name, Version, [.. Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Catalog(defaultLocale, locales)))]);
    }

    internal static bool TryRead(ArtifactRenderInput input, out Catalog? catalog)
    {
        catalog = null;
        if (input.Name != Name || input.Version != Version || input.Sha256 != Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(input.Bytes.AsSpan())))
        {
            return false;
        }

        try
        {
            var serialized = JsonSerializer.Deserialize<DictionaryCatalog>(new UTF8Encoding(false, true).GetString(input.Bytes.AsSpan()));
            if (serialized is null || serialized.Locales is null || !LocalePattern.IsMatch(serialized.DefaultLocale) ||
                !serialized.Locales.ContainsKey(serialized.DefaultLocale) ||
                serialized.Locales.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != serialized.Locales.Count ||
                serialized.Locales.Any(locale => !LocalePattern.IsMatch(locale.Key) || locale.Value?.Any(entry => !KeyPattern.IsMatch(entry.Key) ||
                    entry.Value?.Any(character => char.IsControl(character) || char.IsSurrogate(character)) != false ||
                    !StringsFileEntryValid(entry.Key, entry.Value)) != false))
            {
                return false;
            }

            var value = new Catalog(serialized.DefaultLocale, new SortedDictionary<string, SortedDictionary<string, string>>(StringComparer.Ordinal));
            foreach (var (locale, entries) in serialized.Locales)
            {
                value.Locales.Add(locale, new SortedDictionary<string, string>(entries, StringComparer.Ordinal));
            }

            if (!input.Bytes.SequenceEqual(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))))
            {
                return false;
            }

            catalog = value;
            return true;
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException or ArgumentException or RegexMatchTimeoutException)
        {
            return false;
        }
    }

    internal static Catalog? From(ArtifactRenderProfile profile) =>
        profile.Inputs.FirstOrDefault(_ => _.Name == Name) is { } input && TryRead(input, out var catalog) ? catalog : null;

    internal static string ConfigureProgram(string source, Catalog catalog)
    {
        const string marker = "app.UseCratis();";
        if (source.Split(marker, StringSplitOptions.None).Length != 2)
        {
            throw UnsupportedSemanticRendering.For(nameof(StringsCatalogInput), "Program.cs request localization hook");
        }

        var supported = string.Join(", ", catalog.Locales.Keys.Select(locale => $"System.Globalization.CultureInfo.GetCultureInfo({CSharpCodeBuilder.StringLiteral(locale)})"));
        var localization = $"app.UseRequestLocalization(new Microsoft.AspNetCore.Builder.RequestLocalizationOptions\n{{\n    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(System.Globalization.CultureInfo.InvariantCulture, System.Globalization.CultureInfo.GetCultureInfo({CSharpCodeBuilder.StringLiteral(catalog.DefaultLocale)})),\n    SupportedCultures = [System.Globalization.CultureInfo.InvariantCulture],\n    SupportedUICultures = [{supported}]\n}});\n";
        return source.Replace(marker, localization + marker, StringComparison.Ordinal);
    }

    internal static string Render(Catalog catalog, string rootNamespace)
    {
        var builder = new CSharpCodeBuilder().Namespace(rootNamespace).Using("System.Globalization")
            .OpenBlock("public static class GeneratedStrings")
            .Line($"const string DefaultLocale = {CSharpCodeBuilder.StringLiteral(catalog.DefaultLocale)};")
            .Line("static readonly Dictionary<string, Dictionary<string, string>> Locales = new(StringComparer.OrdinalIgnoreCase)")
            .Line("{");
        foreach (var (locale, entries) in catalog.Locales)
        {
            builder.Line($"    [{CSharpCodeBuilder.StringLiteral(locale)}] = new Dictionary<string, string>(StringComparer.Ordinal)")
                .Line("    {");
            foreach (var (key, value) in entries)
            {
                builder.Line($"        [{CSharpCodeBuilder.StringLiteral(key)}] = {CSharpCodeBuilder.StringLiteral(value)},");
            }

            builder.Line("    },");
        }

        builder.Line("};").BlankLine()
            .Summary("Resolves a modeled string key for the current UI culture, falling back to the default locale.")
            .Line("public static string Resolve(string reference)")
            .Line("{")
            .Line("    var key = reference[\"$strings.\".Length..];")
            .Line("    for (var culture = CultureInfo.CurrentUICulture; !string.IsNullOrEmpty(culture.Name); culture = culture.Parent)")
            .Line("    {")
            .Line("        if (Locales.TryGetValue(culture.Name, out var localized) && localized.TryGetValue(key, out var value)) return value;")
            .Line("    }")
            .Line("    return Locales[DefaultLocale][key];")
            .Line("}")
            .EndBlock();
        return builder.ToString();
    }

    static bool StringsFileEntryValid(string key, string value)
    {
        try
        {
            return StringsFile.Parse($"{key} = \"{value}\"").Entries.Count() == 1;
        }
        catch (InvalidStringsLine)
        {
            return false;
        }
    }

    static InvalidCratisBackendApplicationScaffold Invalid() => new("The strings catalog must contain well-formed .strings files and a declared default locale without duplicate keys.");

    internal sealed record Catalog(string DefaultLocale, SortedDictionary<string, SortedDictionary<string, string>> Locales)
    {
        internal bool Contains(string reference) => Locales[DefaultLocale].ContainsKey(reference["$strings.".Length..]);
    }

    sealed record DictionaryCatalog(string DefaultLocale, Dictionary<string, Dictionary<string, string>> Locales);
}
