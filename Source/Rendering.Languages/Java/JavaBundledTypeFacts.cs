// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Builds the JDK 21 and 25 deep-name union; the source summaries and conservative choices are documented in JavaTypeFacts.Bundled.md.
/// </summary>
static class JavaBundledTypeFacts
{
    internal static JavaTypeFacts Create()
    {
        var types = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<string>>(StringComparer.Ordinal);
        var empty = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal);
        foreach (var name in JavaLangTypes.Names)
        {
            types.Add("java.lang." + name, empty);
        }

        foreach (var name in KnownNestedAndCommonTypes())
        {
            types.Add(name, empty);
        }

        types["java.lang.Enum"] = Names("EnumDesc");
        types["java.lang.Thread"] = Names("Builder OfPlatform OfVirtual State UncaughtExceptionHandler MAX_PRIORITY MIN_PRIORITY NORM_PRIORITY");
        types["java.lang.Thread.Builder"] = Names("OfPlatform OfVirtual");
        types["java.lang.Thread.Builder.OfPlatform"] = Names("OfPlatform OfVirtual");
        types["java.lang.Thread.Builder.OfVirtual"] = Names("OfPlatform OfVirtual");
        types["java.lang.ProcessHandle"] = Names("Info");
        types["java.lang.StringTemplate"] = Names("Processor Linkage RAW STR");
        types["java.lang.StringTemplate.Processor"] = Names("Linkage");
        types["java.lang.ProcessBuilder"] = Names("Type PIPE INHERIT DISCARD");
        types["java.lang.ProcessBuilder.Redirect"] = Names("Type PIPE INHERIT DISCARD");
        types["java.lang.System"] = Names("Level");
        types["java.lang.System.Logger"] = Names("Level");
        types["java.lang.Class"] = Names("OfField OfMethod");
        types["java.lang.invoke.TypeDescriptor"] = Names("OfField OfMethod");
        types["java.lang.invoke.TypeDescriptor.OfField"] = Names("OfField OfMethod");
        types["java.lang.invoke.TypeDescriptor.OfMethod"] = Names("OfField OfMethod");
        types["java.math.BigDecimal"] = Names("ZERO ONE TWO TEN ROUND_UP ROUND_DOWN ROUND_CEILING ROUND_FLOOR ROUND_HALF_UP ROUND_HALF_DOWN ROUND_HALF_EVEN ROUND_UNNECESSARY");
        return new(types.ToImmutable());
    }

    static ImmutableHashSet<string> Names(string names) => names.Split(' ').ToImmutableHashSet(StringComparer.Ordinal);

    static IEnumerable<string> KnownNestedAndCommonTypes() =>
    [
        "java.lang.Character.Subset", "java.lang.Character.UnicodeBlock", "java.lang.Character.UnicodeScript",
        "java.lang.Enum.EnumDesc", "java.lang.ModuleLayer.Controller",
        "java.lang.ProcessBuilder.Redirect", "java.lang.ProcessBuilder.Redirect.Type", "java.lang.ProcessHandle.Info",
        "java.lang.Runtime.Version", "java.lang.ScopedValue.CallableOp", "java.lang.ScopedValue.Carrier",
        "java.lang.StackWalker.Option", "java.lang.StackWalker.StackFrame",
        "java.lang.StringTemplate.Processor", "java.lang.StringTemplate.Processor.Linkage",
        "java.lang.System.Logger", "java.lang.System.Logger.Level", "java.lang.System.LoggerFinder",
        "java.lang.Thread.Builder", "java.lang.Thread.Builder.OfPlatform", "java.lang.Thread.Builder.OfVirtual",
        "java.lang.Thread.State", "java.lang.Thread.UncaughtExceptionHandler",
        "java.lang.invoke.TypeDescriptor", "java.lang.invoke.TypeDescriptor.OfField", "java.lang.invoke.TypeDescriptor.OfMethod",
        "java.lang.annotation.Annotation", "java.util.UUID", "java.util.List", "java.math.BigDecimal",
        "java.time.LocalDate", "java.time.OffsetDateTime"
    ];
}
