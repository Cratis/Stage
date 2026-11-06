// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ProjectionConverter;

/// <summary>
/// Chronicle's visitor replaces IncludeChildren with each child every block, rather than keeping the first one.
/// </summary>
public class when_converting_a_child_with_a_second_every : Specification
{
    Stage.Contracts.Projections.ProjectionDefinition _result = null!;

    void Establish()
    {
        var location = SourceLocation.Start;
        var first = new EverySyntax([], false, AutoMapMode.Inherit, location);
        var second = new EverySyntax([], true, AutoMapMode.Inherit, location);
        var children = new ChildrenSyntax("lines", new PathExpressionSyntax("number", location), AutoMapMode.Inherit, [first, second], location);
        var projection = new ProjectionSyntax("Basket", "Basket", null, AutoMapMode.Inherit, null, [children], location);
        _result = ProjectionConverter.Convert(projection);
    }

    [Fact] void should_use_the_include_children_of_the_last_every() => _result.Children["lines"].All.IncludeChildren.ShouldBeTrue();
}
