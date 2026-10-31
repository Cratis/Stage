// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Admits the render-only, synchronous, typed command payload ABI.</summary>
internal static class SemanticCommandBodyAdmission
{
    static readonly Dictionary<string, string> _ambientTokens = new(StringComparer.Ordinal)
    {
        ["Tenant"] = SemanticContextRuntimeTokens.TenantId,
        ["Identity"] = SemanticContextRuntimeTokens.Identity,
        ["CausedBy"] = SemanticContextRuntimeTokens.CausedBy,
        ["Causation"] = SemanticContextRuntimeTokens.Causation,
        ["Occurred"] = SemanticContextRuntimeTokens.DateTime
    };

    internal static bool IsCSharp(SemanticCommand command, SemanticApplicationContext context) =>
        SemanticCommandHandlerAdmission.Classify(command, context.Request).Kind == SemanticCommandHandlerAdmission.HandlerKind.CSharp;

    internal static void Verify(SemanticCommand command, SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (!HasVerifiedBody(command, context, diagnostics)) return;
        if (!HasUnconditionalOutputs(command))
        {
            diagnostics.Add(Error("STAGE-ESM-019", "Command handlers require unconditional, declared event outputs.", command));
            return;
        }
        var descriptors = context.Request.TypedContextDescriptors.Where(descriptor => descriptor.RequirementId == command.Handler!.RequirementId && descriptor.OperationId == command.Id).ToArray();
        if (descriptors.Length != 1 || !Matches(descriptors[0], command))
        {
            diagnostics.Add(Error("STAGE-ESM-021", "The command handler needs its wrapper-ready Command snapshot descriptor from this compilation.", command));
            return;
        }
        if (command.Properties.Any(property => property.Type.Kind == SemanticTypeReferenceKind.CompositeType))
        {
            diagnostics.Add(Error("STAGE-ESM-015", "Composite command snapshots are not available to command handlers.", command));
            return;
        }
        Analyze(command, context, diagnostics);
    }

    internal static RenderedFile RenderDescriptor(SemanticTypedContextDescriptor descriptor, SemanticApplicationContext context)
    {
        if (!IsCommandDescriptorProbe(descriptor, context)) return SemanticTypedContextRenderer.Render(descriptor, context);
        var command = context.Commands[descriptor.OperationId!.Value];
        var name = $"HandlerContextProbe_{SemanticCommandHandlerRendering.Suffix(command)}";
        var builder = context.CodeBuilder().Namespace(context.TypedContextsNamespace).OpenBlock($"internal static class {name}");
        SemanticCommandHandlerRendering.RenderContext(builder, command, context);
        builder.EndBlock();
        return new(Path.Combine(context.TypedContextsFolder, $"{name}.cs"), builder.ToString());
    }

    internal static IEnumerable<SemanticSpecification> WithoutBodySpecifications(IEnumerable<SemanticSpecification> specifications, SemanticApplicationContext context, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var specification in specifications)
        {
            if (ReachesUnsupportedBody(specification, context))
            {
                diagnostics.Add(new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, $"Specification '{specification.Name}' cannot render: only verified C# command handler bodies can execute in generated command scenarios.", specification.Id));
                continue;
            }
            yield return specification;
        }
    }

    static bool IsCommandDescriptorProbe(SemanticTypedContextDescriptor descriptor, SemanticApplicationContext context) =>
        descriptor.Role == SemanticImplementationRole.CommandHandler && descriptor.OperationId is { } owner &&
        context.Commands.TryGetValue(owner, out var command) && IsCSharp(command, context);

    static bool ReachesUnsupportedBody(SemanticSpecification specification, SemanticApplicationContext context) =>
        ReachesBody(specification, context) && !IsCSharp(context.Commands[specification.When!.Command], context);

    static bool ReachesBody(SemanticSpecification specification, SemanticApplicationContext context) =>
        specification.When is { } action && context.Commands.TryGetValue(action.Command, out var command) && command.Handler is not null;

    static bool HasVerifiedBody(SemanticCommand command, SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (command.Handler is null) return false;
        var classification = SemanticCommandHandlerAdmission.Classify(command, context.Request);
        if (classification.Kind is SemanticCommandHandlerAdmission.HandlerKind.Pending or SemanticCommandHandlerAdmission.HandlerKind.Foreign) return false;
        if (classification.Kind != SemanticCommandHandlerAdmission.HandlerKind.CSharp)
        {
            diagnostics.Add(Error("STAGE-ESM-019", classification.Reason ?? "Invalid command handler attachment.", command));
            return false;
        }
        var inline = SemanticInlineCodeRendering.CommandBody(command, context);
        diagnostics.AddRange(inline.Diagnostics);
        return !inline.Refused;
    }

    static bool HasUnconditionalOutputs(SemanticCommand command) =>
        !command.Produces.IsEmpty && command.Produces.All(produced => produced.When is null && produced.Condition is null);

    static bool Matches(SemanticTypedContextDescriptor descriptor, SemanticCommand command)
    {
        if (!IsHandlerDescriptor(descriptor)) return false;
        var members = descriptor.Members;
        if (members.Any(member => member is not { Source: not null, Type: not null })) return false;
        var commands = members.Where(member => member.Name == "Command").ToArray();
        if (commands.Length != 1) return false;
        return MatchesCommandSnapshot(commands[0], command) && HasOnlyKnownAmbientMembers(members.Where(other => other.Name != "Command"));
    }

    static bool MatchesCommandSnapshot(SemanticTypedContextMember member, SemanticCommand command) =>
        MatchesCommandMember(member, command) && MatchesSnapshotShape(member.Type, command) &&
        member.Type.Properties.SequenceEqual(command.Properties.Select(property => new SemanticContextProperty(property.Name, property.Id, property.Type)));

    // The compiler passes the Command snapshot plus a fixed ambient set; each ambient member is a non-derived runtime context value.
    static bool HasOnlyKnownAmbientMembers(IEnumerable<SemanticTypedContextMember> ambient)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return ambient.All(member => IsKnownAmbientMember(member) && seen.Add(member.Name));
    }

    static bool IsKnownAmbientMember(SemanticTypedContextMember member)
    {
        return member.Name is not null && _ambientTokens.TryGetValue(member.Name, out var token) && IsAmbientMember(member, token);
    }

    static bool IsAmbientMember(SemanticTypedContextMember member, string token) =>
        !member.IsNullable && !member.IsDerived && IsAmbientRuntimeType(member.Type, token) && IsFixedAmbientSource(member);

    static bool IsAmbientRuntimeType(SemanticContextType type, string token) =>
        type.Kind == SemanticContextTypeKinds.Runtime && type.RuntimeToken == token && type.ModelType is null && type.Shape is null && type.Properties.IsEmpty;

    static bool IsFixedAmbientSource(SemanticTypedContextMember member) =>
        member.Source.Kind == SemanticContextSourceKinds.ContextContract && member.Source.SemanticId is null && member.Source.Path == member.Name;

    static bool IsHandlerDescriptor(SemanticTypedContextDescriptor descriptor) =>
        descriptor.IsWrapperReady && descriptor.Role == SemanticImplementationRole.CommandHandler && descriptor.ContextVersion == 1 && !descriptor.Members.IsDefault;

    static bool MatchesCommandMember(SemanticTypedContextMember member, SemanticCommand command) =>
        !member.IsNullable && !member.IsDerived && member.Name == "Command" &&
        member.Source.Kind == SemanticContextSourceKinds.Command && member.Source.SemanticId == command.Id && member.Source.Path == "Command";

    static bool MatchesSnapshotShape(SemanticContextType type, SemanticCommand command) =>
        type.Kind == SemanticContextTypeKinds.Shape && type.Shape == command.Id && type.ModelType is null && type.RuntimeToken is null;

    static void Analyze(SemanticCommand command, SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var builder = new CSharpCodeBuilder();
        SemanticCommandHandlerRendering.Render(builder, command, context);
        var located = context.DeclaringSlice(command.Id);
        var ownNamespace = SliceNaming.Namespace(context.RootNamespace, located.Path);
        var types = new SemanticTypeSystem(context);
        var events = command.Produces.Select(produced => context.Events[produced.EventContract]).DistinctBy(@event => @event.Id);
        var eventTypes = SemanticBodyDeclarations.Events(context, events);
        var imports = SemanticBodyDeclarations.EventNamespaces(context, events);
        var source = SemanticBodyCompilation.ImplicitUsings + string.Join(Environment.NewLine, imports.Select(@namespace => $"using {@namespace};")) + SemanticValidationContextRuntime.AnalysisDeclarations(context) + string.Join(Environment.NewLine, eventTypes) +
            $"namespace {ownNamespace} {{ internal class HandlerProbe {{ {builder} }} }}";
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview));
        var compilation = SemanticBodyCompilation.Create("CommandBodyProbe", tree);
        var model = compilation.GetSemanticModel(tree);
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == SemanticCommandHandlerRendering.Method(command, context));
        var unavailable = method.DescendantNodes().OfType<ExpressionSyntax>().Any(expression =>
            UnavailableAccess(expression, model, SemanticCommandHandlerRendering.ContextType(command, context)));
        if (unavailable)
        {
            diagnostics.Add(Error("STAGE-ESM-015", "The command handler may access only the immutable Command snapshot; ambient context is unavailable.", command));
            return;
        }
        var dynamicBody = method.DescendantNodes().OfType<ExpressionSyntax>().Any(expression => model.GetTypeInfo(expression).Type?.TypeKind == TypeKind.Dynamic);
        if (dynamicBody)
        {
            diagnostics.Add(Error("STAGE-ESM-019", "Dynamic values are not part of the typed command handler ABI.", command));
            return;
        }
        var failures = compilation.GetDiagnostics().Where(SemanticBodyCompilation.IsFailure).ToArray();
        if (failures.Length > 0)
        {
            diagnostics.Add(Error("STAGE-ESM-019", $"The command handler does not satisfy its typed event return contract: {string.Join("; ", failures.Select(diagnostic => diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}", command));
        }
    }

    static bool UnavailableAccess(ExpressionSyntax expression, SemanticModel model, string contextType) => expression switch
    {
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText != "Command" && IsContext(access.Expression, model, contextType),
        ElementAccessExpressionSyntax access => IsContext(access.Expression, model, contextType),
        MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText != "Command" && ConditionalReceiverIsContext(binding, model, contextType),
        ElementBindingExpressionSyntax binding => ConditionalReceiverIsContext(binding, model, contextType),
        _ => false
    };

    static bool IsContext(ExpressionSyntax expression, SemanticModel model, string contextType) => model.GetTypeInfo(expression).Type?.Name == contextType;

    static bool ConditionalReceiverIsContext(ExpressionSyntax binding, SemanticModel model, string contextType) =>
        binding.Ancestors().OfType<ConditionalAccessExpressionSyntax>().FirstOrDefault(access => access.WhenNotNull.Span.Contains(binding.Span)) is { } conditional &&
        IsContext(conditional.Expression, model, contextType);

    static ArtifactRenderDiagnostic Error(string code, string message, SemanticCommand command) => new(code, ArtifactRenderDiagnosticSeverity.Error, message, command.Id);
}
