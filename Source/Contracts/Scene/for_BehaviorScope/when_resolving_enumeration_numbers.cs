// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_BehaviorScope;

public class when_resolving_enumeration_numbers : Specification
{
    BehaviorScope _scope = null!;

    void Establish()
    {
        var parsed = new ScreenplayCompiler().Parse("""
            concept Status : Enum
              open = 1
              closed = 0
            concept Phase : Enum
              draft = -1
              issued = 4
              cancelled = 90
            concept ImplicitStatus : Enum
              open
              closed
            module Work
              feature Processing
                slice StateView List
                  readmodel Row
                    status Status
                    phase Phase
                    implicitStatus ImplicitStatus
                    name String
            """);
        Assert.True(parsed.Success, string.Join(Environment.NewLine, parsed.Diagnostics));
        _scope = BehaviorScope.For(parsed.Value!, []) with
        {
            Subject = new(new(parsed.Value!), [], [new("Row", false, false, SourceLocation.Start)])
        };
    }

    [Fact] void should_resolve_reordered_numbers_in_member_order() => JsonSerializer.Serialize(_scope.EnumerationNumbersOf("item.status")).ShouldEqual("[1,0]");
    [Fact] void should_resolve_sparse_negative_numbers_in_member_order() => JsonSerializer.Serialize(_scope.EnumerationNumbersOf("item.phase")).ShouldEqual("[-1,4,90]");
    [Fact] void should_require_the_declared_field_spelling() => _scope.EnumerationNumbersOf("item.STATUS").ShouldBeNull();
    [Fact] void should_still_resolve_implicit_members() => _scope.EnumerationOf("item.implicitStatus").ShouldNotBeNull();
    [Fact] void should_not_synthesize_implicit_numbers() => _scope.EnumerationNumbersOf("item.implicitStatus").ShouldBeNull();
    [Fact] void should_not_number_non_enumeration_fields() => _scope.EnumerationNumbersOf("item.name").ShouldBeNull();
    [Fact] void should_not_resolve_unknown_fields() => _scope.EnumerationNumbersOf("item.missing").ShouldBeNull();
    [Fact] void should_require_the_exact_subject_prefix() => _scope.EnumerationNumbersOf("Item.status").ShouldBeNull();
    [Fact] void should_require_a_known_subject() => (_scope with { Subject = _scope.Subject! with { Candidates = [new("Missing", false, false, SourceLocation.Start)] } }).EnumerationNumbersOf("item.status").ShouldBeNull();
    [Fact] void should_match_the_member_resolvers_subject_casing() => (_scope with { Subject = _scope.Subject! with { Candidates = [new("row", false, false, SourceLocation.Start)] } }).EnumerationNumbersOf("item.status").ShouldBeNull();
    [Fact] void should_require_a_subject() => (_scope with { Subject = null }).EnumerationNumbersOf("item.status").ShouldBeNull();
    [Fact] void should_allow_an_empty_legacy_scope() => BehaviorScope.None.EnumerationNumbersOf("item.status").ShouldBeNull();
}
