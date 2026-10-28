// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// The direct contribution of one Java file to the closure: its own binder names and the plan files it depends on.
/// </summary>
/// <param name="Fixed">The binder names that need no other file.</param>
/// <param name="Introduced">The fixed names that the model or the facts introduce, without the file's own literal declarations.</param>
/// <param name="Dependencies">The paths of the plan files whose closures flow into this one.</param>
/// <param name="Foreign">The fixed names that do not come from the file's own nested declarations.</param>
/// <param name="Refusal">The unknown-type refusal, when a structured reference is neither declared nor known.</param>
sealed record JavaClosureNode(
    ImmutableHashSet<string> Fixed,
    ImmutableHashSet<string> Introduced,
    ImmutableHashSet<string> Dependencies,
    ImmutableHashSet<string> Foreign,
    LanguagePackRefusal? Refusal)
{
    static readonly string[] _implicitTypes = ["java.lang.Object", "java.lang.Record", "java.lang.Enum", "java.lang.annotation.Annotation"];

    // The pack writes Decimal as plain text "new java.math.BigDecimal(...)", which a kit may follow with an anonymous body. A file that
    // mentions the word BigDecimal (pack Decimal output or a literal) therefore inherits BigDecimal's static members like any literal type.
    static readonly string[] _decimalTypes = ["java.math.BigDecimal"];

    internal static JavaClosureNode Create(JavaFileAnalysis file, JavaPlan plan, JavaTypeFacts facts)
    {
        var references = file.References.Select(reference => Classify(reference, plan, facts)).ToArray();
        var external = references.Where(reference => reference.Names is not null).SelectMany(reference => reference.Names!).ToImmutableHashSet(StringComparer.Ordinal);
        var dependencies = references.Select(reference => reference.Path).Concat(file.Words.All.Select(word => PlanPath(plan, file.Package, word)))
            .OfType<string>().ToImmutableHashSet(StringComparer.Ordinal);
        var inherited = LiteralNames(file, facts);
        return new(
            file.Words.Declaring.Union(file.NestedDeclarations).Union(external).Union(inherited),
            file.NestedDeclarations.Union(external),
            dependencies,
            file.Words.Declaring.Union(external).Union(inherited),
            references.FirstOrDefault(reference => reference.Refusal is not null)?.Refusal);
    }

    static ImmutableHashSet<string> LiteralNames(JavaFileAnalysis file, JavaTypeFacts facts) =>
        file.Words.All.Where(JavaLangTypes.Names.Contains).Select(word => "java.lang." + word).Concat(_implicitTypes)
            .Concat(file.Words.All.Contains("BigDecimal") ? _decimalTypes : [])
            .Select(type => facts.TryGet(type, out var names) ? names : [])
            .Aggregate(ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal), (all, names) => all.Union(names));

    static string? PlanPath(JavaPlan plan, string package, string word) => plan.Declarations.TryGetValue(new(package, word), out var declaration) ? declaration.Path : null;

    static ClassifiedReference Classify(TemplateSymbol reference, JavaPlan plan, JavaTypeFacts facts)
    {
        if (plan.Declarations.TryGetValue(JavaFileAnalysis.Head(reference), out var declaration))
        {
            return new(declaration.Path, null, null);
        }

        var names = ExternalNames(reference, facts);
        return names is null
            ? new(null, null, new(JavaRefusalCodes.UnknownType, $"The type '{reference.Namespace}.{reference.Name}' is neither declared in the plan nor known to the type facts."))
            : new(null, names, null);
    }

    static ImmutableHashSet<string>? ExternalNames(TemplateSymbol reference, JavaTypeFacts facts)
    {
        var segments = reference.Name.Split('.');
        ImmutableHashSet<string>? result = null;
        for (var length = 1; length <= segments.Length; length++)
        {
            if (facts.TryGet(reference.Namespace + "." + string.Join('.', segments[..length]), out var names))
            {
                result = (result ?? ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)).Union(names);
            }
        }

        return result;
    }

    sealed record ClassifiedReference(string? Path, ImmutableHashSet<string>? Names, LanguagePackRefusal? Refusal);
}
