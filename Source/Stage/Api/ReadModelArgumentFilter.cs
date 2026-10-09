// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Api;

/// <summary>
/// Narrows read-model instances by the modeled <c language="csharp">by</c> parameter of a query.
/// </summary>
/// <remarks>
/// A query declared <c language="csharp">CommentsForWorkItem by workItemId</c> answers only the comments of one work item. Serving
/// it from every instance of the read model shows one parent's rows under another, which is what a client sees as
/// stale or leaked data. An argument that is absent or empty is never read as "no filter": the narrowed answer to
/// "the comments of nothing" is no comments.
/// </remarks>
public static class ReadModelArgumentFilter
{
    /// <summary>
    /// Reads the value supplied for a parameter.
    /// </summary>
    /// <param name="arguments">The query arguments, when any were supplied.</param>
    /// <param name="parameter">The parameter name.</param>
    /// <param name="value">The supplied value, when the argument is present at all.</param>
    /// <returns><see langword="true"/> when the caller supplied the argument, empty or not.</returns>
    public static bool TryGetArgument(IEnumerable<KeyValuePair<string, object>>? arguments, string parameter, out string? value)
    {
        value = null;
        if (arguments is null)
        {
            return false;
        }

        foreach (var argument in arguments.Where(argument => string.Equals(argument.Key, parameter, StringComparison.OrdinalIgnoreCase)))
        {
            value = argument.Value?.ToString();

            return true;
        }

        return false;
    }

    /// <summary>
    /// Keeps the instances whose parameter property equals the supplied value.
    /// </summary>
    /// <param name="instances">The read-model instances.</param>
    /// <param name="parameter">The read-model property the query is narrowed by.</param>
    /// <param name="value">The supplied value; absent or empty matches nothing.</param>
    /// <returns>The matching instances.</returns>
    public static IReadOnlyList<object> Matching(IEnumerable<object> instances, string parameter, string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : [.. instances.Where(instance => Matches(instance, parameter, value))];

    static bool Matches(object instance, string parameter, string value)
    {
        if (instance is not DynamicReadModel model)
        {
            return false;
        }

        // The parameter is a property of the read model. When the instance does not carry it, the query is looked
        // up by the instance key - a read model keyed by the very identity the query names.
        var property = model.Values.FirstOrDefault(candidate => string.Equals(candidate.Key, parameter, StringComparison.OrdinalIgnoreCase));
        var actual = property.Key is null ? model.Id : Text(property.Value);

        return string.Equals(actual, value, StringComparison.OrdinalIgnoreCase);
    }

    static string? Text(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => value.GetRawText()
    };
}
