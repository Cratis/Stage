// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Sensitivity;

/// <summary>
/// One node of the sensitivity tree of a command's type graph: what, at each position of a payload or a response, is a
/// secret. Both the semantic type graph and a JSON schema are read into this one shape, so values echoed back to a
/// caller and the messages raised about them follow the same rules whichever representation the command came from.
/// </summary>
/// <remarks>
/// A secret is dropped wherever it sits - top level, nested composite, record field or list element. What the walk
/// cannot see into (a type cut short by recursion, an unreadable schema) is dropped as well, never echoed: when it
/// cannot be shown that a value holds no secret, it is not shown.
/// </remarks>
internal sealed class SensitivityNode
{
    /// <summary>
    /// A position holding an ordinary value.
    /// </summary>
    internal static readonly SensitivityNode Leaf = new(SensitivityKind.Leaf, [], null);

    /// <summary>
    /// A position typed as a secret.
    /// </summary>
    internal static readonly SensitivityNode Secret = new(SensitivityKind.Secret, [], null);

    /// <summary>
    /// A position whose content cannot be seen into, such as a type cut short by recursion.
    /// </summary>
    internal static readonly SensitivityNode Opaque = new(SensitivityKind.Opaque, [], null);

    /// <summary>
    /// A graph that could not be read at all.
    /// </summary>
    internal static readonly SensitivityNode Unreadable = new(SensitivityKind.Unreadable, [], null);

    readonly Dictionary<string, SensitivityNode> _members;
    readonly SensitivityNode? _item;

    SensitivityNode(SensitivityKind kind, Dictionary<string, SensitivityNode> members, SensitivityNode? item)
    {
        Kind = kind;
        _members = members;
        _item = item;
        Carries = kind is SensitivityKind.Secret or SensitivityKind.Unreadable ||
            members.Values.Any(member => member.Carries) ||
            (item?.Carries ?? false);
    }

    /// <summary>
    /// Gets the kind of position this node describes.
    /// </summary>
    internal SensitivityKind Kind { get; }

    /// <summary>
    /// Gets whether the node is, or holds anywhere below it, a secret.
    /// </summary>
    internal bool Carries { get; }

    /// <summary>
    /// Creates the node of a composite - a command, a record or a nested type - from its named members.
    /// </summary>
    /// <param name="members">The members and their nodes.</param>
    /// <returns>The node.</returns>
    internal static SensitivityNode Composite(IEnumerable<(string Name, SensitivityNode Node)> members)
    {
        var named = new Dictionary<string, SensitivityNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, node) in members)
        {
            named[name] = node;
        }

        return new(SensitivityKind.Composite, named, null);
    }

    /// <summary>
    /// Creates the node of a collection.
    /// </summary>
    /// <param name="item">The node of one element.</param>
    /// <returns>The node.</returns>
    internal static SensitivityNode Collection(SensitivityNode item) => new(SensitivityKind.Collection, [], item);

    /// <summary>
    /// Removes the secrets from the members of a payload.
    /// </summary>
    /// <param name="payload">The payload as the caller sent it.</param>
    /// <returns>The payload itself when nothing in it can be secret; otherwise a copy without the secret values.</returns>
    internal IDictionary<string, JsonElement> FilterPayload(IDictionary<string, JsonElement> payload) =>
        Carries ? FilterMembers(payload.Select(member => (member.Key, member.Value))) : payload;

    /// <summary>
    /// Removes the secrets from one value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value without its secrets, or nothing when the whole value is to be dropped.</returns>
    internal JsonElement? Filter(JsonElement value) => Kind switch
    {
        SensitivityKind.Secret or SensitivityKind.Unreadable => null,
        SensitivityKind.Opaque => value.ValueKind is JsonValueKind.Object or JsonValueKind.Array ? null : value,
        SensitivityKind.Collection => FilterCollection(value),
        SensitivityKind.Composite => FilterComposite(value),
        _ => value
    };

    /// <summary>
    /// Gets whether the value at a (possibly dotted) path is a secret, so that nothing derived from it - the value
    /// itself or its length - may reach a message.
    /// </summary>
    /// <param name="path">The property path, with segments separated by dots.</param>
    /// <returns>True when the position is, or is reached through, a secret or a position that cannot be seen into.</returns>
    internal bool IsSecretPath(string path)
    {
        var node = this;
        foreach (var segment in path.Split('.'))
        {
            node = node.Element();
            if (node.Kind is SensitivityKind.Secret or SensitivityKind.Unreadable or SensitivityKind.Opaque)
            {
                return true;
            }

            if (node.Kind == SensitivityKind.Composite && node._members.TryGetValue(segment, out var member))
            {
                node = member;
                continue;
            }

            if (!int.TryParse(segment, out _))
            {
                return false;
            }
        }

        return node.Element().Kind is SensitivityKind.Secret or SensitivityKind.Unreadable;
    }

    // The element node of a collection, however deeply collections are nested; any other node itself.
    SensitivityNode Element() => Kind == SensitivityKind.Collection ? _item!.Element() : this;

    JsonElement? FilterCollection(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return value;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return Carries ? null : value;
        }

        var kept = value.EnumerateArray().Select(item => _item!.Filter(item)).ToArray();
        return kept.Any(item => item is null) ? null : JsonSerializer.SerializeToElement(kept);
    }

    JsonElement? FilterComposite(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return value;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return Carries ? null : value;
        }

        return JsonSerializer.SerializeToElement(FilterMembers(value.EnumerateObject().Select(member => (member.Name, member.Value))));
    }

    Dictionary<string, JsonElement> FilterMembers(IEnumerable<(string Name, JsonElement Value)> members)
    {
        var result = new Dictionary<string, JsonElement>();
        if (Kind != SensitivityKind.Composite)
        {
            return result;
        }

        foreach (var (name, value) in members)
        {
            // A member the graph does not declare cannot be shown to hold no secret, so it is dropped.
            if (_members.TryGetValue(name, out var member) && member.Filter(value) is { } kept)
            {
                result[name] = kept;
            }
        }

        return result;
    }
}
