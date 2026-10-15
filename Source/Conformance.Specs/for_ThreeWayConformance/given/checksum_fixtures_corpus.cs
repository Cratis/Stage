// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class checksum_fixtures_corpus
{
    internal static ExecutableSemanticModel V21
    {
        get
        {
            var model = invoice_model.Compile(Source());
            return ExecutableSemanticModel.Create(LanguageVersion.V21, SemanticVersion.V21, model.Application);
        }
    }

    internal static int Count => Cases.Length + 3;

    // Matches the frozen GenerationFixturesCorpus.V21's distinction between generation shape and authored checksum arithmetic.
    // Pattern/random generation remains refused; nanoid pins exercise checksum rules independently of those future factories.
    static readonly (string Algorithm, string Value, bool Valid)[] Cases =
    [
        ("luhn", "79927398713", true), ("luhn", "79927398714", false),
        ("luhn", "", false), ("luhn", "0", true), ("luhn", "abc0", true),
        ("luhn", " 0", true), ("luhn", "0 ", false), ("luhn", "\t0", false), ("luhn", "é0", false),
        ("mod11", "123456785", true), ("mod11", "123456784", false),
        ("mod11", "60", false), ("mod11", "", false), ("mod11", "0", true),
        ("mod11", "abc0", true), ("mod11", "000000006-A10", false),
        ("mod97", "12345678978", true), ("mod97", "12345678979", false),
        ("mod97", "", false), ("mod97", "8", false), ("mod97", "98", true),
        ("mod97", "abc98", true), ("mod97", "ABC98", true),
        ("mod97", " 98", true), ("mod97", "98 ", false), ("mod97", "\n98", false),
        ("mod97", "6209", true), ("mod97", "62106", false), ("mod97", "A1-B2-C3２０", false)
    ];

    static string Source()
    {
        var source = new StringBuilder();
        foreach (var algorithm in new[] { "luhn", "mod11", "mod97" })
        {
            source.AppendLine($"concept {algorithm}Reference : String\n  validate checksum {algorithm}").AppendLine($"concept {algorithm}Generated : String\n  generate nanoid {(algorithm == "mod97" ? 2 : 1)} alphabet \"098\"\n  validate checksum {algorithm}");
        }
        source.AppendLine("module Shop\n  feature References");
        foreach (var algorithm in new[] { "luhn", "mod11", "mod97" })
        {
            source.AppendLine($"    slice StateChange {algorithm}Check\n      command Check{algorithm}\n        value {algorithm}Reference\n        returns value");
            foreach (var (item, index) in Cases.Select((item, index) => (item, index)).Where(pair => pair.item.Algorithm == algorithm))
            {
                source.AppendLine($"      specification ChecksumCase{index}\n        when Check{algorithm}\n          value = {JsonSerializer.Serialize(item.Value)}").AppendLine(item.Valid
                    ? $"        then returns {JsonSerializer.Serialize(item.Value)}"
                    : $"        then error \"A value must end in the exact '{algorithm}' check digits.\"");
            }
            var pin = algorithm == "mod97" ? "98" : "0";
            source.AppendLine($"    slice StateChange {algorithm}Issue\n      command Issue{algorithm}\n        value {algorithm}Generated generated\n        returns value\n      specification Pinned{algorithm}\n        when Issue{algorithm}\n          generated value = \"{pin}\"\n        then returns \"{pin}\"");
        }
        return source.ToString();
    }
}
#endif
