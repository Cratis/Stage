// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Identifies command validation rules whose meaning this target cannot interpret.
/// </summary>
internal static class SemanticDeniedValidations
{
    internal static bool IsDenied(SemanticValidationRule rule) =>
        !SemanticVersionFeatures.KnownValidationRule(rule.Kind) || !Enum.IsDefined(rule.Severity);

    internal static bool Denies(SemanticCommand command) => command.Validations.Any(IsDenied);

    internal static string Reason(SemanticCommand command) => string.Join("; ", command.Validations.Where(IsDenied).Select(rule =>
        (!SemanticVersionFeatures.KnownValidationRule(rule.Kind) ? $"validation rule kind '{rule.Kind}'" : $"severity '{rule.Severity}'") +
        $" on property '{SafeText(command.Properties.Single(property => property.Id == rule.Property).Name)}' has no generated validator in this target"));

    internal static string Message(SemanticCommand command, string reason) =>
        $"STAGE-DEGRADE-002: Command '{SafeText(command.Name)}' is denied: {reason}.";

    static string SafeText(string text) => string.Concat(text.Select(character =>
        char.IsControl(character) || char.IsSurrogate(character) || character is '{' or '}' ? '_' : character));
}
