// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_BehaviorAttachments.when_converting;

/// <summary>
/// An anonymous behavior cannot be the target of a uses, so it never enters the lookup.
/// </summary>
public class and_a_declared_behavior_has_no_name : Specification
{
    IReadOnlyDictionary<string, BehaviorSyntax> _result = null!;

    void Because() => _result = BehaviorAttachments.Declared(
    [
        new BehaviorSyntax(null, [], [], SourceLocation.Start),
        new BehaviorSyntax("Named", [], [], SourceLocation.Start)
    ]);

    [Fact] void should_only_contain_the_named_one() => _result.Count.ShouldEqual(1);
    [Fact] void should_contain_the_named_one() => _result.ContainsKey("Named").ShouldBeTrue();
}
