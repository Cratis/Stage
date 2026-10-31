// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;

namespace Cratis.Stage.Rendering.Cratis.Execution;

/// <summary>Builds the rendered handler's private context from the portable command and invokes its body.</summary>
internal static class BodyHandlerInvocation
{
    internal const string CommandMember = "Command";

    /// <summary>Invokes one compiled handler against the command snapshot.</summary>
    /// <param name="assembly">The loaded unit.</param>
    /// <param name="unit">The admitted manifest entry.</param>
    /// <param name="request">The request whose single <c lang="csharp">Command</c> member holds the portable command composite.</param>
    /// <returns>The handler's returned event or event tuple.</returns>
    /// <exception cref="InvalidOperationException">The handler context type or entry point is unavailable.</exception>
    internal static object? Invoke(Assembly assembly, BodyUnitEntry unit, BodyEvaluationRequest request)
    {
        var entry = assembly.GetType(unit.EntryType!, throwOnError: true)!;
        var snapshotType = assembly.GetType(unit.WrapperType!, throwOnError: true)!;
        // The catalog names the handler members, so the context type is the entry point's one nested parameter rather than a name prefix.
        var method = entry.GetMethods(BindingFlags.NonPublic | BindingFlags.Static).SingleOrDefault(candidate => string.Equals(candidate.Name, unit.EntryMethod, StringComparison.Ordinal) && candidate.GetParameters().Length == 1 && candidate.GetParameters()[0].ParameterType.DeclaringType == entry)
            ?? throw new InvalidOperationException("The handler entry point is unavailable.");
        var contextType = method.GetParameters()[0].ParameterType;
        var snapshot = BodyValueMaterializer.Wrapper(snapshotType, Members(snapshotType, request, unit), unit);
        return method.Invoke(null, [Activator.CreateInstance(contextType, snapshot)]);
    }

    static SemanticCompositeValue? CommandComposite(BodyEvaluationRequest request) =>
        request.Members.Count == 1 && request.Members.TryGetValue(CommandMember, out var wire) ? SemanticValueWire.Read(wire) as SemanticCompositeValue : null;

    // The request is keyed by semantic property id; the snapshot's constructor is keyed by its camel-cased parameter names.
    static BodyEvaluationRequest Members(Type snapshotType, BodyEvaluationRequest request, BodyUnitEntry unit)
    {
        var command = CommandComposite(request) ?? throw new ArgumentException("A handler request carries exactly one Command composite.");
        var parameters = snapshotType.GetConstructors().Single().GetParameters().Select(parameter => parameter.Name).ToArray();
        var members = ImmutableDictionary.CreateBuilder<string, System.Text.Json.JsonElement>(StringComparer.Ordinal);
        foreach (var property in command.Properties)
        {
            var name = unit.PropertyNames.TryGetValue(property.TargetProperty.ToString(), out var authored)
                ? parameters.SingleOrDefault(parameter => string.Equals(parameter, authored, StringComparison.OrdinalIgnoreCase))
                : null;
            if (name is null || !members.TryAdd(name, SemanticValueWire.Write(property.Value))) throw new ArgumentException("Unknown or repeated command property.");
        }
        return request with { Members = members.ToImmutable() };
    }
}
