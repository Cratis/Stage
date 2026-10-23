// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

// Companion resources for the canonical vectors, not a production fallback for missing catalogs.
internal static class canonical_strings_catalog
{
    internal static IReadOnlyDictionary<string, string> Files { get; } = new Dictionary<string, string>
    {
        ["canonical.en.strings"] = "validation.predicate = \"The value must satisfy the predicate.\"\n"
    };
}
#endif
