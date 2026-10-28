// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Execution;

/// <summary>Compiles admitted rendered validation units. Call only in the isolated worker.</summary>
public static class BodyUnitCompiler
{
    /// <summary>Compiles one immutable model revision and returns explicit verdicts for all requirements.</summary>
    /// <param name="loaded">The resolved immutable semantic model.</param>
    /// <param name="cacheDirectory">The compiled unit cache.</param>
    /// <returns>The admission and compilation manifest.</returns>
    public static BodyUnitManifest Compile(LoadedSemanticModel loaded, string cacheDirectory) => Compile(loaded, cacheDirectory, static () => { });

    internal static BodyUnitManifest Compile(LoadedSemanticModel loaded, string cacheDirectory, Action emitting)
    {
        var context = CreateContext(loaded);
        return new(loaded.Model.Revision.ToString(), [.. loaded.ImplementationRequirements.Select(requirement => CompileUnit(context, requirement, cacheDirectory, emitting))]);
    }

    internal static SemanticApplicationContext CreateContext(LoadedSemanticModel loaded)
    {
        var name = Identifiers.ToPascalCase(loaded.Model.Application.Name);
        var options = new CratisRenderingOptions(name, name);
        var request = new ArtifactRenderRequest(loaded.Model, loaded.Plan, CratisRendering.CreateProfile(name, options), new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            TypedContextDescriptors = loaded.TypedContextDescriptors,
            ImplementationContents = loaded.ImplementationContents,
            AttachmentDiagnostics = loaded.AttachmentDiagnostics
        };
        return new(request, options);
    }

    static BodyUnitEntry CompileUnit(SemanticApplicationContext context, SemanticImplementationRequirement requirement, string cacheDirectory, Action emitting)
    {
        var (descriptor, body, refusal) = BodyUnitAdmission.Admit(context, requirement);
        if (refusal is not null) return refusal;
        try
        {
            var sources = BodyUnitSources.Render(context, descriptor!, body!);
            var references = References();
            var hash = Hash(context, descriptor!, sources, references);
            var cached = BodyUnitCache.Read(cacheDirectory, hash);
            if (cached is not null) return cached;
            var compilation = CSharpCompilation.Create(
                $"BodyUnit_{hash}",
                sources.Select(source => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp14))),
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release, deterministic: true, nullableContextOptions: NullableContextOptions.Enable));
            using var stream = new MemoryStream();
            emitting();
            var emitted = compilation.Emit(stream);
            if (!emitted.Success) return BodyUnitAdmission.Refuse(requirement, "STAGE-ESM-021", "Rendered declaration compilation failed: " + emitted.Diagnostics.First(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).GetMessage(System.Globalization.CultureInfo.InvariantCulture));
            var pe = stream.ToArray();
            var entry = new BodyUnitEntry(requirement.RequirementId, requirement.Role.ToString(), true, UnitHash: hash, PeSha256: Convert.ToHexString(SHA256.HashData(pe)), EntryType: $"{context.TypedContextsNamespace}.ValidationBodies", EntryMethod: SemanticValidationContextRuntime.Method(descriptor!), WrapperType: $"{context.TypedContextsNamespace}.{SemanticValidationContextRuntime.Wrapper(descriptor!)}")
            {
                BodySha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body!))),
                PropertyNames = PropertyNames(context),
                PropertyPaths = PropertyPaths(context, descriptor!)
            };
            BodyUnitCache.Write(cacheDirectory, entry, pe);
            return entry;
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or IOException)
        {
            return BodyUnitAdmission.Refuse(requirement, "STAGE-ESM-021", "Rendered compilation declarations or pinned references are unavailable.");
        }
    }

    static MetadataReference[] References()
    {
        // Only emission gets Chronicle metadata; BodyUnitAdmission still uses the renderer's unchanged analysis references.
        var chronicle = Assembly.Load("Cratis.Chronicle");
        return [.. PureTransitionAdmission.AnalysisReferences(), SemanticGenerationBodyAdmission.FundamentalsReference, MetadataReference.CreateFromFile(chronicle.Location)];
    }

    static string Hash(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, string[] sources, MetadataReference[] references)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(SemanticModelSerializer.Serialize(context.Request.Model));
        Add(JsonSerializer.Serialize(context.Request.ImplementationRequirements));
        Add(JsonSerializer.Serialize(descriptor));
        Add(typeof(BodyUnitCompiler).Module.ModuleVersionId.ToString());
        Add(typeof(SemanticApplication).Assembly.FullName!);
        Add(typeof(CSharpCompilation).Assembly.FullName!);
        Add($"{BodyWorkerMessages.ProtocolVersion}:{PureTransitionAdmission.ReferencePackVersion}:CSharp14:Release:NullableEnable:Deterministic");
        foreach (var source in sources) Add(source);
        foreach (var reference in references.Where(reference => File.Exists(reference.Display))) hash.AppendData(SHA256.HashData(File.ReadAllBytes(reference.Display!)));
        return Convert.ToHexString(hash.GetHashAndReset());

        void Add(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            hash.AppendData(BitConverter.GetBytes(bytes.Length));
            hash.AppendData(bytes);
        }
    }

    static ImmutableDictionary<string, string> PropertyNames(SemanticApplicationContext context) => context.Commands.Values.SelectMany(command => command.Properties)
        .Concat(context.Application.Types.SelectMany(type => type.Properties)).DistinctBy(property => property.Id)
        .ToImmutableDictionary(property => property.Id.ToString(), context.MemberNames.Property);

    static ImmutableDictionary<string, string> PropertyPaths(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor) =>
        descriptor.OperationId is { } operation && context.Commands.TryGetValue(operation, out var command)
            ? command.Properties.ToImmutableDictionary(property => property.Name, context.MemberNames.WireProperty, StringComparer.Ordinal)
            : [];
}
