// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Removes only recognized clock members whose exact path and owner identify an audited UTC command rule.
/// </summary>
internal static class SemanticCommandClockAdmission
{
    static readonly Dictionary<string, string> _clockPaths = new(StringComparer.Ordinal)
    {
        ["SemanticClockOperand.Amount"] = "Amount",
        ["SemanticClockOperand.DateOf"] = "DateOf",
        ["SemanticClockOperand.Kind"] = "Kind",
        ["SemanticClockOperand.Unit"] = "Unit",
        ["SemanticClockKind.Now"] = "Kind",
        ["SemanticClockUnit.Day"] = "Unit",
        ["SemanticClockUnit.Hour"] = "Unit",
        ["SemanticClockUnit.Minute"] = "Unit",
        ["SemanticConditionOperand.Clock"] = string.Empty,
        ["SemanticValidationRule.ClockOperand"] = string.Empty
    };

    static readonly Dictionary<string, string> _applicationPaths = new(StringComparer.Ordinal)
    {
        ["SemanticApplication.TimeZone"] = "Application.TimeZone",
        ["SemanticTimeZone.Id"] = "Application.TimeZone.Id",
        ["SemanticTimeZone.Configurable"] = "Application.TimeZone.Configurable"
    };

    enum ClockRoot
    {
        Either,
        Condition,
        Validation
    }

    internal static EsmSupportStanding Known(EsmSupportStanding standing, ExecutableSemanticModel model) =>
        standing with { UnknownMembers = [.. standing.UnknownMembers.Where(member => !Admits(model, member))] };

    static bool Admits(ExecutableSemanticModel model, SemanticUnknownMember member)
    {
        if (string.IsNullOrEmpty(member.Path)) return false;
        if (_applicationPaths.TryGetValue(member.Member, out var applicationPath)) return IsApplicationMember(model.Application, member, applicationPath);
        if (!_clockPaths.TryGetValue(member.Member, out var suffix)) return false;
        var root = member.Member switch
        {
            "SemanticConditionOperand.Clock" => ClockRoot.Condition,
            "SemanticValidationRule.ClockOperand" => ClockRoot.Validation,
            _ => ClockRoot.Either
        };
        var owner = new FindingPath(member.Path.Split('.'), suffix, root).Resolve(model.Application);
        return owner is not null && MatchesOwner(owner, member.Owner) && SemanticCommandClockSupport.Supports(model.Application, owner.Command);
    }

    static bool IsApplicationMember(SemanticApplication application, SemanticUnknownMember member, string path) =>
        member.Owner == application.Id && string.Equals(member.Path, path, StringComparison.Ordinal) && SemanticCommandClockSupport.DefaultUtc(application);

    static bool MatchesOwner(CommandOwner owner, SemanticId identity) => identity == owner.Command.Id || identity == owner.Slice.Id;

    sealed record CommandOwner(SemanticSlice Slice, SemanticCommand Command);

    /// <summary>
    /// Follows the property/index grammar emitted by SemanticUnknownMembers.Walker; no identity-based search or reflection fallback.
    /// </summary>
    /// <param name="segments">The dot-separated segments of the finding path.</param>
    /// <param name="suffix">The expected leaf segment after the clock segment, or empty when the finding is the clock itself.</param>
    /// <param name="root">The positions in which the member can represent a clock.</param>
    sealed class FindingPath(string[] segments, string suffix, ClockRoot root)
    {
        internal CommandOwner? Resolve(SemanticApplication application)
        {
            if (!Has(0, "Application")) return null;
            var module = At(application.Modules, 1, "Modules");
            var feature = module is null ? null : At(module.Features, 2, "Features");
            return feature is null ? null : InFeature(feature, 3);
        }

        static bool Index(string segment, string name, out int index)
        {
            index = 0;
            if (!segment.StartsWith(name + "[", StringComparison.Ordinal) || !segment.EndsWith(']')) return false;
            var digits = segment.AsSpan(name.Length + 1, segment.Length - name.Length - 2);
            return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out index) && (digits.Length == 1 || digits[0] != '0');
        }

        CommandOwner? InFeature(SemanticFeature feature, int position)
        {
            var nested = At(feature.Features, position, "Features");
            if (nested is not null) return InFeature(nested, position + 1);
            var slice = At(feature.Slices, position, "Slices");
            return slice is null ? null : InSlice(slice, position + 1);
        }

        CommandOwner? InSlice(SemanticSlice slice, int position)
        {
            var command = At(slice.Commands, position, "Commands");
            if (command is null) return null;
            var rule = At(command.Validations, position + 1, "Validations");
            if (rule is not null) return InValidation(rule, position + 2) ? new(slice, command) : null;
            var condition = Condition(command, position + 1);
            return condition is not null && InCondition(condition, position + 3) ? new(slice, command) : null;
        }

        SemanticCondition? Condition(SemanticCommand command, int position)
        {
            var requirement = At(command.Requirements, position, "Requirements");
            if (requirement is not null) return Has(position + 1, "Condition") ? requirement.Condition : null;
            var produced = At(command.Produces, position, "Produces");
            return Has(position + 1, "When") ? produced?.When : null;
        }

        bool InCondition(SemanticCondition condition, int position)
        {
            if (condition is SemanticLogicalCondition logical) return InLogical(logical, position);
            if (condition is not SemanticComparison comparison) return false;
            var operand = Side(comparison.Left, comparison.Right, position);
            return operand is not null && Clock(operand, position + 1);
        }

        bool InLogical(SemanticLogicalCondition logical, int position)
        {
            var nested = Side(logical.Left, logical.Right, position);
            return nested is not null && InCondition(nested, position + 1);
        }

        bool InValidation(SemanticValidationRule rule, int position) =>
            root != ClockRoot.Condition && rule.ClockOperand is not null && Has(position, "ClockOperand") && EndClock(position);

        bool Clock(SemanticConditionOperand operand, int position) =>
            root != ClockRoot.Validation && operand.Clock is not null && Has(position, "Clock") && EndClock(position);

        bool EndClock(int position) => suffix.Length == 0
            ? segments.Length == position + 1
            : segments.Length == position + 2 && Has(position + 1, suffix);

        T? Side<T>(T left, T right, int position)
            where T : class => Segment(position) switch
        {
            "Left" => left,
            "Right" => right,
            _ => null
        };

        T? At<T>(ImmutableArray<T> values, int position, string name)
            where T : class
        {
            if (values.IsDefaultOrEmpty || position >= segments.Length) return null;
            return Index(segments[position], name, out var index) && index < values.Length ? values[index] : null;
        }

        bool Has(int position, string value) => string.Equals(Segment(position), value, StringComparison.Ordinal);

        string? Segment(int position) => position < segments.Length ? segments[position] : null;
    }
}
