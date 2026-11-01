// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Stage.Rendering.Kits;

static class KitJson
{
    internal static JsonSerializerOptions Options { get; } = CreateOptions();

    static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            AllowDuplicateProperties = false,
            RespectRequiredConstructorParameters = true,
            RespectNullableAnnotations = true,
            MaxDepth = 32,
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        options.Converters.Add(new KitVersionConverter<ContractVersion>(text => ContractVersion.TryParse(text, out var version) ? version : new(text, -1, -1), version => version.ToString()));
        options.Converters.Add(new KitVersionConverter<LanguagePackVersion>(text => LanguagePackVersion.TryParse(text, out var version) ? version : new(string.Empty, text), version => version.ToString()));
        options.MakeReadOnly();
        return options;
    }
}
