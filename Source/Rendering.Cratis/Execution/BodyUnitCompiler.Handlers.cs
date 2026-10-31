// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Execution;

public static partial class BodyUnitCompiler
{
    static BodyUnitEntry CompileHandler(SemanticApplicationContext context, SemanticImplementationRequirement requirement, string cacheDirectory, Action emitting)
    {
        try
        {
            var (command, refusal) = BodyUnitAdmission.AdmitHandler(context, requirement);
            if (refusal is not null) return refusal;
            var unit = BodyUnitSources.RenderHandler(context, command!);
            return BuildHandler(new(context, requirement, command!), unit, cacheDirectory, emitting);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or IOException)
        {
            return BodyUnitAdmission.Refuse(requirement, "STAGE-ESM-021", "Command handler compilation or pinned references are unavailable.");
        }
    }

    static BodyUnitEntry BuildHandler(HandlerBuild build, BodyUnitSources.HandlerSources unit, string cacheDirectory, Action emitting)
    {
        var (context, requirement, command) = build;

        // The worker compiles with the very references render admission analyzed the handler against.
        var references = PureTransitionAdmission.AnalysisReferences().ToArray();
        var hash = Hash(context, "retained-handler-v2:" + command.Id, new([unit.Source, unit.Path, unit.EntryType, unit.EntryMethod, "retained-handler-v2"], references, nameof(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)));
        var cached = BodyUnitCache.Read(cacheDirectory, hash);
        if (cached is not null) return cached;
        var tree = CSharpSyntaxTree.ParseText(unit.Source, new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview), unit.Path);
        var compilation = CSharpCompilation.Create($"BodyUnit_{hash}", [tree], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release, generalDiagnosticOption: ReportDiagnostic.Error, deterministic: true, nullableContextOptions: NullableContextOptions.Enable));
        using var stream = new MemoryStream();
        emitting();
        var emitted = compilation.Emit(stream);
        if (!emitted.Success) return BodyUnitAdmission.Refuse(requirement, "STAGE-ESM-021", "Rendered handler compilation failed: " + emitted.Diagnostics.First(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).GetMessage(System.Globalization.CultureInfo.InvariantCulture));
        var pe = stream.ToArray();
        var entry = new BodyUnitEntry(requirement.RequirementId, requirement.Role.ToString(), true, UnitHash: hash, PeSha256: Convert.ToHexString(SHA256.HashData(pe)), EntryType: unit.EntryType, EntryMethod: unit.EntryMethod, WrapperType: unit.SnapshotType)
        {
            BodySha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(BodyOf(context, requirement, command)))),
            PropertyNames = HandlerProperties(context, command),
            StateMembers = StateProperties(context).ToImmutableDictionary(member => $"{member.Type}.{Identifiers.ToPascalCase(member.Property.Name)}", member => member.Property.Id.ToString()),
            EnumValues = EnumValues(context),
            EventTypes = [.. context.Events.Values.Select(@event => SliceType(context, @event.Id, @event.Name)).Distinct(StringComparer.Ordinal)]
        };
        BodyUnitCache.Write(cacheDirectory, entry, pe);
        return entry;
    }

    static string BodyOf(SemanticApplicationContext context, SemanticImplementationRequirement requirement, SemanticCommand command)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        return SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, requirement.RequirementId, command.Id, diagnostics, out var body) ? body! : throw new InvalidOperationException("The verified handler body is unavailable.");
    }

    // The command's own members are matched to snapshot constructor parameters by name, the rest map composite and event members.
    static ImmutableDictionary<string, string> HandlerProperties(SemanticApplicationContext context, SemanticCommand command) =>
        StateProperties(context).Select(member => member.Property).Concat(command.Properties).DistinctBy(property => property.Id)
            .ToImmutableDictionary(property => property.Id.ToString(), property => Identifiers.ToPascalCase(property.Name));

    sealed record HandlerBuild(SemanticApplicationContext Context, SemanticImplementationRequirement Requirement, SemanticCommand Command);
}
