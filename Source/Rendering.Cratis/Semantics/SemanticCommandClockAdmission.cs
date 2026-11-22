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
    static readonly string[] _conditionMembers =
    [
        "SemanticClockOperand.Amount", "SemanticClockOperand.DateOf", "SemanticClockOperand.Kind", "SemanticClockOperand.Unit",
        "SemanticClockKind.Now", "SemanticClockUnit.Day", "SemanticClockUnit.Hour", "SemanticClockUnit.Minute", "SemanticConditionOperand.Clock"
    ];

    static readonly string[] _applicationMembers = ["SemanticApplication.TimeZone", "SemanticTimeZone.Id", "SemanticTimeZone.Configurable"];

    [GeneratedRegex(@"\.Commands\[(?<index>[0-9]+)\]", RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CommandSegment { get; }

    internal static EsmSupportStanding Known(EsmSupportStanding standing, ExecutableSemanticModel model) =>
        standing with { UnknownMembers = [.. standing.UnknownMembers.Where(member => !Admits(model, member))] };

    static bool Admits(ExecutableSemanticModel model, SemanticUnknownMember member)
    {
        if (_applicationMembers.Contains(member.Member, StringComparer.Ordinal)) return member.Owner == model.Application.Id && SemanticCommandClockSupport.DefaultUtc(model.Application);
        if (!_conditionMembers.Contains(member.Member, StringComparer.Ordinal)) return false;
        var command = Owner(model.Application, member);
        return command is not null && SemanticCommandClockSupport.Supports(model.Application, command);
    }

    static SemanticCommand? Owner(SemanticApplication application, SemanticUnknownMember member)
    {
        var match = CommandSegment.Match(member.Path);
        if (!match.Success) return null;
        var index = int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture);
        return SemanticVersionFeatures.Slices(application)
            .Select(slice => OwnedCommand(slice, index, member.Owner))
            .FirstOrDefault(command => command is not null);
    }

    static SemanticCommand? OwnedCommand(SemanticSlice slice, int index, SemanticId owner)
    {
        var command = slice.Commands.ElementAtOrDefault(index);
        if (slice.Id == owner) return command;
        return command?.Id == owner ? command : null;
    }
}
