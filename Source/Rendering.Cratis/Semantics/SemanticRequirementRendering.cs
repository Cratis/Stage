// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders command-property requirements as command-wide validation failures.
/// </summary>
internal static class SemanticRequirementRendering
{
    internal static bool CanRender(SemanticRequirement requirement, SemanticCommand command, SemanticApplicationContext context) =>
        SemanticValidationRendering.CanResolve(requirement.Message, context) && Enum.IsDefined(requirement.Severity) &&
        SemanticConditionRendering.CanRender(requirement.Condition, command.Properties, context, reads: new SemanticReadScope(command, context));

    /// <summary>
    /// Renders a requirement of a read-protected command as a guard inside Handle, so it is evaluated with the decision reads.
    /// </summary>
    /// <param name="builder">The code builder.</param>
    /// <param name="requirement">The requirement.</param>
    /// <param name="command">The command that declares it.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="reads">The command's read scope.</param>
    /// <param name="result">The checked result type the guard returns through.</param>
    /// <param name="clock">The active command receipt instant for clock comparisons.</param>
    /// <exception cref="UnsupportedSemanticRendering">The requirement is not an error, which admission refuses first.</exception>
    internal static void RenderGuard(CSharpCodeBuilder builder, SemanticRequirement requirement, SemanticCommand command, SemanticApplicationContext context, SemanticReadScope reads, string result, string? clock = null)
    {
        // Screenplay defines no semantics for other severities over protected reads; admission refuses them (STAGE-ESM-045).
        if (requirement.Severity != SemanticValidationSeverity.Error) throw UnsupportedSemanticRendering.For(nameof(SemanticValidationSeverity), requirement.Severity);

        var condition = SemanticConditionRendering.Render(requirement.Condition, new(command.Properties, string.Empty, context, property => SemanticCommandResultRendering.PropertyValue(property, context), Reads: reads, Clock: clock));
        var message = requirement.Message ?? "Command requirement was not met.";
        var error = message.StartsWith("$strings.", StringComparison.Ordinal)
            ? $"global::Cratis.Arc.Validation.ValidationResult.Error(global::{context.RootNamespace}.GeneratedStrings.Resolve({CSharpCodeBuilder.StringLiteral(message)}), state: {CSharpCodeBuilder.StringLiteral(message)})"
            : $"global::Cratis.Arc.Validation.ValidationResult.Error({CSharpCodeBuilder.StringLiteral(message)})";
        builder.Line($"if (!{condition}) return {result}.FromT1({error});");
    }

    internal static void Render(CSharpCodeBuilder builder, SemanticRequirement requirement, SemanticCommand command, SemanticApplicationContext context, string? clock = null)
    {
        var condition = SemanticConditionRendering.Render(requirement.Condition, new(command.Properties, "command", context, property => $"command.{context.MemberNames.Property(property)}", Clock: clock));
        var message = requirement.Message ?? "Command requirement was not met.";
        var severitySuffix = SeveritySuffix(requirement.Severity);
        var parameters = clock is null ? "command" : "(command, _, validation)";
        if (message.StartsWith("$strings.", StringComparison.Ordinal))
        {
            builder.Line($"RuleFor(_ => _).Must({parameters} => {condition}).WithMessage(_ => global::{context.RootNamespace}.GeneratedStrings.Resolve({CSharpCodeBuilder.StringLiteral(message)})).WithState({CSharpCodeBuilder.StringLiteral(message)}){severitySuffix};");
        }
        else
        {
            builder.Line($"RuleFor(_ => _).Must({parameters} => {condition}).WithMessage({CSharpCodeBuilder.StringLiteral(message)}){severitySuffix};");
        }
    }

    static string SeveritySuffix(SemanticValidationSeverity severity) => severity switch
    {
        SemanticValidationSeverity.Error => string.Empty,
        SemanticValidationSeverity.Warning => ".WithSeverity(global::Cratis.Arc.Validation.ValidationResultSeverity.Warning)",
        SemanticValidationSeverity.Information => ".WithSeverity(global::Cratis.Arc.Validation.ValidationResultSeverity.Information)",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticValidationSeverity), severity)
    };
}
