// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Screens;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_CompositionConverter;

public class when_converting_template_metadata : Specification
{
    TemplateMetadata _unrestricted = null!;
    TemplateMetadata _restrictedToNothing = null!;
    TemplateMetadata _restricted = null!;
    Exception? _unknown;

    void Because()
    {
        _unrestricted = CompositionConverter.Metadata("masterDetail", "feature", restrictsScopes: false, []);
        _restrictedToNothing = CompositionConverter.Metadata("masterDetail", "feature", restrictsScopes: true, ["none"]);
        _restricted = CompositionConverter.Metadata(null, null, restrictsScopes: true, ["module", "slice"]);
        _unknown = Catch.Exception(() => CompositionConverter.Metadata(null, null, restrictsScopes: true, ["galaxy"]));
    }

    [Fact] void should_leave_a_template_without_scopes_usable_anywhere() => _unrestricted.Scopes.ShouldBeNull();
    [Fact] void should_keep_scopes_none_distinct_from_no_scopes() => _restrictedToNothing.Scopes!.ShouldBeEmpty();
    [Fact] void should_convert_each_declared_scope() => _restricted.Scopes!.ShouldEqual(TemplateScope.Module, TemplateScope.Slice);
    [Fact] void should_carry_the_template_type() => _unrestricted.Type.ShouldEqual("masterDetail");
    [Fact] void should_refuse_an_unknown_scope() => _unknown.ShouldBeOfExactType<UnknownCompositionValue>();
}
