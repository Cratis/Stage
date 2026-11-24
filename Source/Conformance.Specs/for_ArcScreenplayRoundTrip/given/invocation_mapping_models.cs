// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Screenplay.Conformance;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

// Normalized structural references, not raw semantic IDs: exercise the local rewrite independently of graph validation.
internal static class invocation_mapping_models
{
    public const string FirstTarget = "/application/modules[Banking]/features[Deposits]/slices[Deposit]/commands[Deposit]/properties[first]";
    public const string SecondTarget = "/application/modules[Banking]/features[Deposits]/slices[Deposit]/commands[Deposit]/properties[second]";

    public static NormalizedSemanticNode Assignment(string target, string source, string root = "event", string sourceKind = "property") => Object(
        ("targetProperty", Text($"@{target}")),
        ("source", Object(("kind", Text("resolved")), ("root", Text(root)), ("source", Text(sourceKind)), ("target", Text(source)))));

    public static NormalizedSemanticNode LiteralAssignment(string value) => Object(
        ("targetProperty", Text($"@{FirstTarget}")), ("source", Object(("kind", Text("literal")), ("value", Text(value)))));

    public static NormalizedSemanticNode PathAssignment(bool reverse)
    {
        var path = new[] { Text("first"), Text("second") };
        return Object(("targetProperty", Text($"@{FirstTarget}")), ("source", Object(("kind", Text("resolved")), ("path", NormalizedSemanticNode.List(reverse ? path.Reverse() : path)))));
    }

    public static NormalizedSemanticNode[] Assignments => [Assignment(FirstTarget, FirstTarget), Assignment(SecondTarget, SecondTarget)];

    public static NormalizedSemanticModel Model(IEnumerable<NormalizedSemanticNode> mappings)
    {
        var invocation = Object(("command", Text("Deposit")), ("mappings", NormalizedSemanticNode.List(mappings)));
        var trigger = Object(("invokes", NormalizedSemanticNode.List([invocation])));
        return new(Object(("reactions", Reactions(NormalizedSemanticNode.List([trigger])))), []);
    }

    public static NormalizedSemanticModel Ordered(string owner, bool reverse)
    {
        var items = new[] { Object(("name", Text("first"))), Object(("name", Text("second"))) };
        var list = NormalizedSemanticNode.List(reverse ? items.Reverse() : items);
        var trigger = Object(("invokes", owner == "invokes" ? list : NormalizedSemanticNode.List([])), ("produces", owner == "produces" ? list : NormalizedSemanticNode.List([])));
        return new(
            Object(
                ("reactions", Reactions(owner == "triggers" ? list : NormalizedSemanticNode.List([trigger]))),
                ("projections", Object(("mappings", owner == "projection mappings" ? list : NormalizedSemanticNode.List([]))))),
            []);
    }

    public static NormalizedSemanticNode Object(params (string Key, NormalizedSemanticNode Value)[] members) =>
        NormalizedSemanticNode.Object(members.Select(member => new KeyValuePair<string, NormalizedSemanticNode>(member.Key, member.Value)));

    static NormalizedSemanticNode Reactions(NormalizedSemanticNode triggers) => NormalizedSemanticNode.Set([new("Follow", Object(("triggers", triggers)))]);

    static NormalizedSemanticNode Text(string value) => NormalizedSemanticNode.Leaf(JsonSerializer.Serialize(value));
}
#endif
