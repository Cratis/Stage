// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Stage.Rendering.Templates;

internal static class FrozenHelperVectors
{
    static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    internal static ImmutableArray<HelperVector> All { get; } = Load();

    internal static ImmutableArray<HelperVector> For(params string[] ids) => [.. ids.Select(id => All.Single(vector => vector.Id == id))];

    static ImmutableArray<HelperVector> Load()
    {
        using var stream = typeof(HelperCatalogue).Assembly.GetManifestResourceStream("Cratis.Stage.Rendering.Templates.v1.vectors.json")!;
        return JsonSerializer.Deserialize<ImmutableArray<HelperVector>>(stream, _options);
    }
}
