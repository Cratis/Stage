// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_the_case_plans_cannot_be_spelled : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _unknown;
    ImmutableArray<DeclarativeFileResolution> _collision;

    void Because()
    {
        _unknown = new JavaLanguagePack(K1GoldenCases.Facts()).Resolve(K1GoldenCases.Files());
        _collision = Resolve(Java("Order.java", "example", "Order"), Java("order.java", "example", "order"));
    }

    [Fact] void should_refuse_a_type_that_is_neither_declared_nor_known() => Code(_unknown[4]).ShouldEqual(JavaRefusalCodes.UnknownType);
    [Fact] void should_refuse_top_level_names_that_differ_only_in_case() => _collision.ShouldEachConformTo(resolution => Code(resolution) == JavaRefusalCodes.CaseCollision);
}
