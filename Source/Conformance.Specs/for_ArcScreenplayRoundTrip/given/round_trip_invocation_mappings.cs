// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Screenplay.Conformance;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

// Run after identity normalization. Only a reaction trigger's command invocation assignments are target-keyed.
internal static class round_trip_invocation_mappings
{
    const string InvocationMappings = "/reactions/triggers/invokes/mappings";

    public static NormalizedSemanticModel Normalize(NormalizedSemanticModel model) => model with { Root = Rewrite(model.Root, string.Empty) };

    static NormalizedSemanticNode Rewrite(NormalizedSemanticNode node, string ownerPath)
    {
        if (ownerPath.EndsWith(InvocationMappings, StringComparison.Ordinal)) return ByTarget(node, ownerPath);

        switch (node.Kind)
        {
            case NormalizedSemanticNodeKind.Object:
                return NormalizedSemanticNode.Object(node.Members.Select(member =>
                    new KeyValuePair<string, NormalizedSemanticNode>(member.Key, Rewrite(member.Value, $"{ownerPath}/{member.Key}"))));
            case NormalizedSemanticNodeKind.Set:
                return NormalizedSemanticNode.Set(node.Members.Select(member =>
                    new KeyValuePair<string, NormalizedSemanticNode>(member.Key, Rewrite(member.Value, ownerPath))));
            case NormalizedSemanticNodeKind.List:
                return NormalizedSemanticNode.List(node.Items.Select(item => Rewrite(item, ownerPath)));
            default:
                return node;
        }
    }

    static NormalizedSemanticNode ByTarget(NormalizedSemanticNode node, string path)
    {
        if (node.Kind == NormalizedSemanticNodeKind.Set) return node;
        if (node.Kind != NormalizedSemanticNodeKind.List) throw new SemanticNormalizationFailed(path, "Invocation mappings must be a list.");

        var assignments = new Dictionary<string, NormalizedSemanticNode>(StringComparer.Ordinal);
        foreach (var mapping in node.Items)
        {
            var target = Target(mapping, path);
            if (!assignments.TryAdd(target, mapping)) throw new SemanticNormalizationFailed(path, $"Duplicate invocation mapping target '{target}'.");
        }
        return NormalizedSemanticNode.Set(assignments);
    }

    static string Target(NormalizedSemanticNode mapping, string path)
    {
        if (mapping.Kind != NormalizedSemanticNodeKind.Object) throw new SemanticNormalizationFailed(path, "Invocation mapping has no resolved target property.");
        if (!mapping.Members.TryGetValue("targetProperty", out var target)) throw new SemanticNormalizationFailed(path, "Invocation mapping has no resolved target property.");
        if (target.Kind != NormalizedSemanticNodeKind.Leaf) throw new SemanticNormalizationFailed(path, "Invocation mapping has no resolved target property.");

        var key = JsonSerializer.Deserialize<string>(target.Value);
        if (key?.StartsWith("@/application/", StringComparison.Ordinal) != true) throw new SemanticNormalizationFailed(path, "Invocation mapping target is not a structural property reference.");
        return key[1..];
    }
}
#endif
