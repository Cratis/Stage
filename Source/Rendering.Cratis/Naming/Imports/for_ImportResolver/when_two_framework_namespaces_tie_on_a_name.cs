// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_two_framework_namespaces_tie_on_a_name : given.a_framework_catalog
{
    readonly TypeReference _alphaShared = new("Cratis.Chronicle.Alpha", "Shared", 0, ReferenceContext.Type);
    readonly TypeReference _betaShared = new("Cratis.Chronicle.Beta", "Shared", 0, ReferenceContext.Type);
    readonly TypeReference _alphaOnly = new("Cratis.Chronicle.Alpha", "AlphaOnly", 0, ReferenceContext.Type);
    readonly TypeReference _alphaExtra = new("Cratis.Chronicle.Alpha", "AlphaExtra", 0, ReferenceContext.Type);
    readonly TypeReference _betaOnly = new("Cratis.Chronicle.Beta", "BetaOnly", 0, ReferenceContext.Type);
    ImportResolution _forward = null!;
    ImportResolution _reversed = null!;
    ImportResolution _tie = null!;
    ImportResolution _tieReversed = null!;

    void Because()
    {
        _forward = ImportResolver.Resolve(Request(_alphaShared, _betaShared, _alphaOnly, _alphaExtra, _betaOnly), _catalog);
        _reversed = ImportResolver.Resolve(Request(_betaOnly, _alphaExtra, _alphaOnly, _betaShared, _alphaShared), _catalog);
        _tie = ImportResolver.Resolve(Request(_betaShared, _alphaShared), _catalog);
        _tieReversed = ImportResolver.Resolve(Request(_alphaShared, _betaShared), _catalog);
    }

    [Fact] void should_keep_the_namespace_with_more_short_references_short() => _forward.Spell(_alphaShared).ShouldEqual("Shared");
    [Fact] void should_qualify_the_name_in_the_losing_namespace() => _forward.Spell(_betaShared).ShouldEqual("global::Cratis.Chronicle.Beta.Shared");
    [Fact] void should_keep_the_other_names_of_the_losing_namespace_short() => _forward.Spell(_betaOnly).ShouldEqual("BetaOnly");
    [Fact] void should_import_the_winner_inside_the_namespace() => string.Join(' ', _forward.Usings).ShouldEqual("Cratis.Chronicle.Alpha");
    [Fact] void should_import_the_loser_above_the_namespace() => string.Join(' ', _forward.TopLevelUsings).ShouldEqual("Cratis.Chronicle.Beta");
    [Fact] void should_not_depend_on_reference_order() => string.Join(' ', _reversed.Usings.Concat(_reversed.TopLevelUsings)).ShouldEqual(string.Join(' ', _forward.Usings.Concat(_forward.TopLevelUsings)));
    [Fact] void should_spell_the_same_when_reversed() => _reversed.Spell(_betaShared).ShouldEqual(_forward.Spell(_betaShared));
    [Fact] void should_prefer_the_ordinal_first_namespace_on_a_full_tie() => _tie.Spell(_alphaShared).ShouldEqual("Shared");
    [Fact] void should_qualify_the_other_on_a_full_tie() => _tie.Spell(_betaShared).ShouldEqual("global::Cratis.Chronicle.Beta.Shared");
    [Fact] void should_resolve_a_full_tie_independent_of_order() => _tieReversed.Spell(_betaShared).ShouldEqual(_tie.Spell(_betaShared));
}
