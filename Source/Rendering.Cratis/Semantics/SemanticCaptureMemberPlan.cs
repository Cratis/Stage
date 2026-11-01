// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The immutable declaration contract shared by capture admission, inventory and emission.
/// </summary>
internal sealed class SemanticCaptureMemberPlan
{
    static readonly string[] _roles = ["Previous", "Observe", "Overlay", "SameState", "Changed", "Text", "ConceptValue", "Field", "Same", "MissingCaptureField", "EmptyCaptureKey", "CaptureKeyIsNotTheEventSource"];
    readonly ImmutableDictionary<string, string> _members;
    readonly ImmutableDictionary<SemanticId, string> _handlers;

    SemanticCaptureMemberPlan(string typeName, ImmutableDictionary<string, string> members, ImmutableDictionary<SemanticId, string> handlers, ImmutableArray<string> refusals)
    {
        TypeName = typeName;
        _members = members;
        _handlers = handlers;
        Refusals = refusals;
        Store = new SemanticCaptureMemberAllocator([], members.Values.Concat(handlers.Values).Append(typeName)).Allocate("eventStore");
    }

    internal string TypeName { get; }
    internal string Store { get; }
    internal ImmutableArray<string> Refusals { get; }
    internal string this[string role] => _members[role];

    internal static SemanticCaptureMemberPlan Create(SemanticCapture capture, SemanticApplicationContext context)
    {
        var refusals = ImmutableArray.CreateBuilder<string>();
        var typeName = Identifiers.ToPascalCase(capture.Name);
        if (!SemanticCaptureIdentifier.TryDecode(typeName, out _)) refusals.Add($"generated type name '{typeName}' is not a C# identifier");
        var sources = (capture.EventsSource?.Events ?? []).Where(context.Events.ContainsKey).Select(id => context.Events[id]).ToArray();
        var handlers = sources.Select(source =>
        {
            var spelling = Identifiers.ToPascalCase(source.Name);
            if (!SemanticCaptureIdentifier.TryDecode(spelling, out var identity)) refusals.Add($"generated source event name '{spelling}' is not a C# identifier");
            var qualified = $"{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(source.Id).Path)}.{spelling}";
            return (source.Id, Preferred: $"On{identity}", Signature: SemanticCaptureIdentifier.Qualified(qualified));
        }).ToArray();
        if (sources.Select(source => source.Id).Distinct().Count() != sources.Length) refusals.Add("a source event is repeated in its inbox dispatch declarations");
        if (handlers.Select(handler => handler.Signature).Distinct(StringComparer.Ordinal).Count() != handlers.Length) refusals.Add("source events have the same emitted inbox handler parameter type");
        var allocator = new SemanticCaptureMemberAllocator(_roles.Concat(handlers.Select(handler => handler.Preferred)), [typeName]);
        var members = _roles.ToImmutableDictionary(role => role, allocator.Allocate, StringComparer.Ordinal);
        var names = ImmutableDictionary.CreateBuilder<SemanticId, string>();
        foreach (var group in handlers.GroupBy(handler => handler.Preferred, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var name = allocator.Allocate(group.Key);
            foreach (var handler in group.OrderBy(handler => handler.Signature, StringComparer.Ordinal).ThenBy(handler => handler.Id.ToString(), StringComparer.Ordinal)) names[handler.Id] = name;
        }
        return new(typeName, members, names.ToImmutable(), refusals.ToImmutable());
    }

    internal string Handler(SemanticId source) => _handlers[source];
}
