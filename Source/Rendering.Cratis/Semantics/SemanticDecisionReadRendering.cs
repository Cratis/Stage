// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders the protected decision reads of a command as Arc read-protected Handle parameters and absence guards.
/// </summary>
/// <remarks>
/// Arc resolves each nullable read-model parameter through <c>[LookUpBy]</c> and guards the first event it reads.
/// Absence is rendered explicitly, because Arc's own refusal of a missing read model is not a validation failure
/// and does not carry the reference message.
/// </remarks>
/// <param name="command">The command whose reads are rendered.</param>
/// <param name="context">The indexed semantic application.</param>
/// <param name="locals">The command's shared Handle name allocator.</param>
internal sealed class SemanticDecisionReadRendering(SemanticCommand command, SemanticApplicationContext context, SemanticHandleLocals locals)
{
    /// <summary>
    /// The attribute that turns Arc read protection on for one command.
    /// </summary>
    internal const string ProtectionAttribute = "global::Cratis.Arc.Commands.ReadProtectionAttribute(global::Cratis.Arc.Commands.ReadProtectionMode.On)";

    readonly SemanticTypeSystem _types = new(context);

    /// <summary>
    /// Gets whether the command declares protected reads.
    /// </summary>
    internal bool HasReads => !command.Reads.IsDefaultOrEmpty;

    /// <summary>
    /// Gets the scope read-backed conditions and mappings resolve against.
    /// </summary>
    internal SemanticReadScope Scope { get; } = new(command, context, locals);

    /// <summary>
    /// Renders the read-model parameters in declaration order.
    /// </summary>
    /// <returns>The parameter declarations.</returns>
    internal IEnumerable<string> Parameters() => command.Reads.Select(read =>
    {
        var key = command.Properties.Single(property => property.Id == read.Key);
        var view = context.ReadModels[read.ReadModel];
        var commandName = Identifiers.ToPascalCase(command.Name);

        return $"[global::Cratis.Arc.Commands.LookUpByAttribute(nameof({commandName}.{Identifiers.ToPascalCase(key.Name)}))] {_types.SliceType(view.Id, view.Name)}? {Scope.Local(read.Name)}";
    });

    /// <summary>
    /// Renders the absence guards in declaration order, before any requirement.
    /// </summary>
    /// <param name="builder">The code builder.</param>
    /// <param name="result">The checked result type the guards return through.</param>
    internal void RenderAbsenceGuards(CSharpCodeBuilder builder, string result)
    {
        foreach (var read in command.Reads)
        {
            var view = context.ReadModels[read.ReadModel];
            var message = $"{CSharpCodeBuilder.StringLiteral($"{view.Name} '")} + {KeyText(read)} + {CSharpCodeBuilder.StringLiteral("' does not exist.")}";
            builder.Line($"if ({Scope.Local(read.Name)} is null) return {result}.FromT1(global::Cratis.Arc.Validation.ValidationResult.Error({message}));");
        }
    }

    /// <summary>
    /// Renders a read-path expression over a guarded read local.
    /// </summary>
    /// <param name="path">The read path.</param>
    /// <returns>The C# value expression.</returns>
    /// <exception cref="UnsupportedSemanticRendering">The read or its property is not declared by the command.</exception>
    internal string ReadPath(SemanticReadPathExpression path)
    {
        var property = Scope.Property(path.Read, path.PropertyId) ?? throw UnsupportedSemanticRendering.For(nameof(SemanticReadPathExpression), path.Read);

        return $"{Scope.Local(path.Read)}.{Identifiers.ToPascalCase(property.Name)}";
    }

    string KeyText(SemanticDecisionRead read)
    {
        var key = command.Properties.Single(property => property.Id == read.Key);
        var value = SemanticCommandResultRendering.PropertyValue(key);

        return key.Type.Kind == SemanticTypeReferenceKind.Concept ? $"{value}.Value" : value;
    }
}
