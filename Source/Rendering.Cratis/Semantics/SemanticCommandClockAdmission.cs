// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Removes only recognized clock members whose owning command has the audited UTC condition shape.
/// </summary>
internal static partial class SemanticCommandClockAdmission
{
    static readonly HashSet<string> _conditionMembers = new(StringComparer.Ordinal)
    {
        "SemanticClockOperand.Amount", "SemanticClockOperand.DateOf", "SemanticClockOperand.Kind", "SemanticClockOperand.Unit",
        "SemanticClockKind.Now", "SemanticClockUnit.Day", "SemanticClockUnit.Hour", "SemanticClockUnit.Minute", "SemanticConditionOperand.Clock"
    };

    static readonly HashSet<string> _applicationMembers = new(StringComparer.Ordinal)
    {
        "SemanticApplication.TimeZone", "SemanticTimeZone.Id", "SemanticTimeZone.Configurable"
    };

    internal static EsmSupportStanding Known(EsmSupportStanding standing, ExecutableSemanticModel model) =>
        standing with { UnknownMembers = [.. standing.UnknownMembers.Where(member => !Admits(model, member))] };

    static bool Admits(ExecutableSemanticModel model, SemanticUnknownMember member)
    {
        if (_applicationMembers.Contains(member.Member)) return member.Owner == model.Application.Id && SemanticCommandClockSupport.DefaultUtc(model.Application);
        if (!_conditionMembers.Contains(member.Member)) return false;
        var command = Owner(model.Application, member);
        return command is not null && SemanticCommandClockSupport.Supports(model.Application, command);
    }

    static SemanticCommand? Owner(SemanticApplication application, SemanticUnknownMember member)
    {
        var match = CommandSegment().Match(member.Path);
        if (!match.Success) return null;
        var slice = SemanticVersionFeatures.Slices(application).FirstOrDefault(candidate => candidate.Id == member.Owner);
        return slice?.Commands.ElementAtOrDefault(int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture));
    }

    [GeneratedRegex(@"\.Commands\[(?<index>[0-9]+)\]", RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CommandSegment();
}
