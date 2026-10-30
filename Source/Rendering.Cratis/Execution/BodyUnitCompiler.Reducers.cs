// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Execution;

/// <summary>Compiles reducer transitions from exactly the compilation their pure analysis accepted.</summary>
public static partial class BodyUnitCompiler
{
    internal static BodyUnitEntry CompileReducer(SemanticApplicationContext context, SemanticImplementationRequirement requirement, string cacheDirectory, (Action Emitting, Func<SemanticApplicationContext, SemanticTypedContextDescriptor, string, BodyUnitSources.ReducerSources> Render) compilation)
    {
        try
        {
            var (descriptor, body, refusal) = BodyUnitAdmission.AdmitReducer(context, requirement);
            if (refusal is not null) return refusal;
            var unit = compilation.Render(context, descriptor!, body!);
            MetadataReference[] references = [.. PureTransitionAdmission.AnalysisReferences(), SemanticGenerationBodyAdmission.FundamentalsReference, SemanticGenerationBodyAdmission.ChronicleReference];
            var build = new UnitBuild(unit.Sources, references, Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest, (hash, peSha) =>
                new BodyUnitEntry(requirement.RequirementId, requirement.Role.ToString(), true, UnitHash: hash, PeSha256: peSha, EntryType: unit.EntryType, EntryMethod: unit.EntryMethod, WrapperType: $"{context.TypedContextsNamespace}.{SemanticTypedContextRenderer.WrapperName(descriptor!)}")
                {
                    BodySha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body!))),
                    PropertyNames = StateProperties(context).DistinctBy(member => member.Property.Id).ToImmutableDictionary(member => member.Property.Id.ToString(), member => Identifiers.ToPascalCase(member.Property.Name)),
                    StateMembers = StateProperties(context).ToImmutableDictionary(member => $"{member.Type}.{Identifiers.ToPascalCase(member.Property.Name)}", member => member.Property.Id.ToString()),
                    EnumValues = EnumValues(context)
                });
            return BuildReducer(new(context, requirement, descriptor!, cacheDirectory, compilation.Emitting), unit, build);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or IOException)
        {
            return BodyUnitAdmission.Refuse(requirement, "STAGE-ESM-021", "Analysed reducer compilation or pinned references are unavailable.");
        }
    }

    internal static string ReducerHash(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, BodyUnitSources.ReducerSources unit, MetadataReference[] references)
    {
        var identity = string.Join(":", "retained-reducer-v1", unit.Path, unit.EntryType, unit.EntryMethod, EmbeddedHash("Fundamentals"), EmbeddedHash("Chronicle"));
        return Hash(context, descriptor, new([.. unit.Sources, identity], references, nameof(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest)));
    }

    static BodyUnitEntry CompileReducer(SemanticApplicationContext context, SemanticImplementationRequirement requirement, string cacheDirectory, Action emitting) =>
        CompileReducer(context, requirement, cacheDirectory, (emitting, BodyUnitSources.RenderReducer));

    static BodyUnitEntry BuildReducer(UnitScope scope, BodyUnitSources.ReducerSources unit, UnitBuild build)
    {
        var hash = ReducerHash(scope.Context, scope.Descriptor, unit, build.References);
        var trees = build.Sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(build.Language), index == 0 ? unit.Path : $"worker/declarations/{index}.cs"));
        var compilation = CSharpCompilation.Create($"BodyUnit_{hash}", trees, build.References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release, generalDiagnosticOption: ReportDiagnostic.Error, deterministic: true, nullableContextOptions: NullableContextOptions.Enable));
        var verdict = PureTransitionAdmission.AnalyzeRendered(compilation, unit.Path, scope.Descriptor, scope.Context);
        if (!verdict.Accepted) return BodyUnitAdmission.Refuse(scope.Requirement, verdict.Code!, verdict.Reason!);
        return BodyUnitCache.Read(scope.CacheDirectory, hash) ?? EmitUnit(scope, build, hash, compilation);
    }

    static string EmbeddedHash(string name)
    {
        using var stream = typeof(SemanticGenerationBodyAdmission).Assembly.GetManifestResourceStream($"GenerationReferences.Cratis.{name}.dll")
            ?? throw new InvalidOperationException($"Missing {name} reference.");
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    // The rendered declarations name every read-model, event and composite member by its Pascal-cased property name.
    static IEnumerable<(string Type, SemanticProperty Property)> StateProperties(SemanticApplicationContext context) =>
        context.ReadModels.Values.SelectMany(model => model.Properties.Select(property => (Type: SliceType(context, model.Id, model.Name), Property: property)))
            .Concat(context.Events.Values.SelectMany(@event => @event.Properties.Select(property => (Type: SliceType(context, @event.Id, @event.Name), Property: property))))
            .Concat(context.Application.Types.SelectMany(type => type.Properties.Select(property => (Type: $"{context.CommonNamespace}.{Identifiers.ToPascalCase(type.Name)}", Property: property))))
            .DistinctBy(member => (member.Type, member.Property.Id));

    static string SliceType(SemanticApplicationContext context, SemanticId id, string name) =>
        $"{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(id).Path)}.{Identifiers.ToPascalCase(name)}";

    static ImmutableDictionary<string, string> EnumValues(SemanticApplicationContext context) =>
        context.Application.Concepts.Where(concept => !concept.Values.IsEmpty)
            .SelectMany(concept => concept.Values.Select(value => (Key: $"{context.CommonNamespace}.{Identifiers.ToPascalCase(concept.Name)}.{Identifiers.ToPascalCase(value)}", Value: value)))
            .ToImmutableDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    sealed record UnitScope(SemanticApplicationContext Context, SemanticImplementationRequirement Requirement, SemanticTypedContextDescriptor Descriptor, string CacheDirectory, Action Emitting);

    sealed record HashInput(string[] Sources, MetadataReference[] References, string Language);

    sealed record UnitBuild(string[] Sources, MetadataReference[] References, Microsoft.CodeAnalysis.CSharp.LanguageVersion Language, Func<string, string, BodyUnitEntry> Entry);
}
