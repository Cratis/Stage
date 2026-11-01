// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_allocating_deterministic_aliases : given.a_framework_catalog
{
    [Fact]
    void should_reuse_aliases_and_reserve_generated_and_pinned_identifiers_independent_of_request_order()
    {
        TypeReference[] references = [new("System", "Guid", 0, ReferenceContext.Type), new("System.Text.RegularExpressions", "Regex", 0, ReferenceContext.Type)];
        var request = Request(references) with { ScopeTypeNames = ["Guid", "Regex", "StageImport0"], PinnedNames = ["StageImport1"] };
        var result = ImportResolver.Resolve(request, _catalog);
        var reverse = ImportResolver.Resolve(request with { References = [.. references.Reverse(), references[0]] }, _catalog);
        Assert.Equal("StageImport2::Guid", result.Spell(references[0]));
        Assert.Equal("StageImport3::Regex", result.Spell(references[1]));
        Assert.Equal(new[] { "StageImport2 = global::System", "StageImport3 = global::System.Text.RegularExpressions" }, result.Aliases.ToArray());
        Assert.Equal(result.Aliases.ToArray(), reverse.Aliases.ToArray());
        Assert.Equal(result.Spell(references[0]), reverse.Spell(references[0]));
    }
}
