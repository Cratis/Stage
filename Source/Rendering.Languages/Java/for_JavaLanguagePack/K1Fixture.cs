// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public static class K1Fixture
{
    const string ResourcePrefix = "Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.Vectors.k1.";

    public static SymbolPlan Load()
    {
        using var document = JsonDocument.Parse(Read("plan.json"));
        var root = document.RootElement;
        var types = root.GetProperty("types").EnumerateArray().Select(Type).ToArray();
        var files = root.GetProperty("files").EnumerateArray().ToArray();
        var owners = files.ToDictionary(file => file.GetProperty("id").GetString()!, file => file.GetProperty("owner").GetString()!, StringComparer.Ordinal);
        var declarations = files.SelectMany(file => file.GetProperty("segments").EnumerateArray()
            .Where(segment => segment[0].GetString() == "d").Select(segment => (Type: segment[1].GetInt32(), Owner: owners[file.GetProperty("id").GetString()!])))
            .ToDictionary(item => item.Type, item => item.Owner);
        var symbols = files.Select(file => File(file, types, declarations)).ToArray();
        var external = types.Where((_, index) => !declarations.ContainsKey(index)).ToArray();

        // External members and ancestry deliberately stay Unknown. The frozen files have no explicit supertypes.
        return new(symbols, external);
    }

    public static string Read(string name)
    {
        using var stream = typeof(K1Fixture).Assembly.GetManifestResourceStream(ResourcePrefix + name) ?? throw new InvalidOperationException("Missing K1 resource " + name);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    static TypeName Type(JsonElement element) => new(element[0].GetString()!, element[1].GetString()!, [.. element[2].EnumerateArray().Select(item => item.GetString()!)]);

    static SymbolFile File(JsonElement file, TypeName[] types, IReadOnlyDictionary<int, string> declarations)
    {
        var owner = file.GetProperty("owner").GetString()!;
        var segments = file.GetProperty("segments").EnumerateArray().Select(segment => Segment(segment, types, declarations, owner)).ToArray();

        return new(file.GetProperty("id").GetString()!, file.GetProperty("package").GetString()!, segments);
    }

    static SymbolSegment Segment(JsonElement segment, TypeName[] types, IReadOnlyDictionary<int, string> declarations, string owner)
    {
        var kind = segment[0].GetString();
        if (kind == "t") return new TextSegment(segment[1].GetString()!);
        if (kind == "i") return new ImportsSegment();
        var index = segment[1].GetInt32();
        var targetOwner = declarations.GetValueOrDefault(index, string.Empty);

        return kind switch
        {
            "d" => new DeclarationSegment(types[index], owner) { Supertypes = KnownTypes<TypeName>.None },
            "n" => new NameSegment(types[index], targetOwner),
            "r" => new ReferenceSegment(types[index], targetOwner, owner),
            _ => throw new InvalidOperationException("Unknown K1 segment " + kind),
        };
    }
}
