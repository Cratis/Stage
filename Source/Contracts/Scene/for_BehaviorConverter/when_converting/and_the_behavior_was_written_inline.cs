// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;
using SceneModel = Cratis.Scene.Model.Interactions;

namespace Cratis.Stage.Contracts.Scene.for_BehaviorConverter.when_converting;

/// <summary>
/// An inline block is the same construct with no name. Anonymity has to survive the translation, or the
/// renderer cannot tell an attachment written in place from one that was named and reused.
/// </summary>
public class and_the_behavior_was_written_inline : Specification
{
    SceneModel.Behavior _result = null!;

    void Because() => _result = BehaviorConverter.Convert(new BehaviorSyntax(null, [], [], SourceLocation.Start));

    [Fact] void should_stay_anonymous() => _result.Name.ShouldBeNull();
    [Fact] void should_have_no_order() => _result.Order.ShouldBeNull();
}
