// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;
using SceneModel = Cratis.Scene.Model.Interactions;

namespace Cratis.Stage.Contracts.Scene.for_BehaviorAttachments.when_converting;

/// <summary>
/// Inline blocks and uses clauses arrive in separate collections, so authored order has to be recovered from
/// source location. Order matters: a confirm written before an execute is only a gate if it stays before it.
/// </summary>
public class and_inline_blocks_are_mixed_with_uses_clauses : Specification
{
    IReadOnlyList<SceneModel.Behavior> _result = null!;
    List<RenderFinding> _findings = null!;

    void Establish() => _findings = [];

    void Because() => _result = BehaviorAttachments.Convert(
        [new BehaviorSyntax(null, [], [], new SourceLocation(20, 5))],
        [new UsesBehaviorSyntax("ConfirmFirst", [], new SourceLocation(10, 5))],
        BehaviorAttachments.Declared([new BehaviorSyntax("ConfirmFirst", [], [], SourceLocation.Start)]),
        "InvoiceList",
        _findings);

    [Fact] void should_attach_both() => _result.Count.ShouldEqual(2);
    [Fact] void should_put_the_one_written_first_first() => _result[0].Name.ShouldEqual("ConfirmFirst");
    [Fact] void should_put_the_inline_block_second() => _result[1].Name.ShouldBeNull();
    [Fact] void should_report_nothing() => _findings.ShouldBeEmpty();
}
