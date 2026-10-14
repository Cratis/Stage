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
        SemanticConditionRendering.CanRender(requirement.Condition, command.Properties, context);

    internal static void Render(CSharpCodeBuilder builder, SemanticRequirement requirement, SemanticCommand command, SemanticApplicationContext context)
    {
        var condition = SemanticConditionRendering.Render(requirement.Condition, new(command.Properties, "command", context));
        var message = requirement.Message ?? "Command requirement was not met.";
        var severity = requirement.Severity switch
        {
            SemanticValidationSeverity.Error => "Error",
            SemanticValidationSeverity.Warning => "Warning",
            SemanticValidationSeverity.Information => "Information",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticValidationSeverity), requirement.Severity)
        };
        var severitySuffix = severity == "Error" ? string.Empty : $".WithSeverity(global::Cratis.Arc.Validation.ValidationResultSeverity.{severity})";
        if (message.StartsWith("$strings.", StringComparison.Ordinal))
        {
            builder.Line($"RuleFor(_ => _).Must(command => {condition}).WithMessage(_ => global::{context.RootNamespace}.GeneratedStrings.Resolve({CSharpCodeBuilder.StringLiteral(message)})).WithState({CSharpCodeBuilder.StringLiteral(message)}){severitySuffix};");
        }
        else
        {
            builder.Line($"RuleFor(_ => _).Must(command => {condition}).WithMessage({CSharpCodeBuilder.StringLiteral(message)}){severitySuffix};");
        }
    }
}
