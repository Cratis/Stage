// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_loading_a_protected_read_model : given.a_model_path
{
    List<StageUnsupportedIssue> _semanticIssues = [];
    List<StageUnsupportedIssue> _legacyIssues = [];
    LoadedSemanticModel? _semantic;
    object? _legacy;

    async Task Because()
    {
        await File.WriteAllTextAsync(_path, a_protected_invoice_model.Source);
        (_semantic, _) = await SemanticHost.LoadModel(_path, _semanticIssues);
        _legacy = await EventModelHost.LoadModel(_path, _legacyIssues);
    }

    [Fact] void should_load_in_the_semantic_engine() => _semantic.ShouldNotBeNull();
    [Fact] void should_report_no_semantic_issue() => _semanticIssues.ShouldBeEmpty();
    [Fact] void should_refuse_in_the_eventmodel_engine() => _legacy.ShouldBeNull();
    [Fact] void should_name_the_refusal() => _legacyIssues.Single().Details.ShouldContain("STAGE-ESM-044:");
    [Fact] void should_name_the_engine() => _legacyIssues.Single().Details.ShouldContain("eventmodel");
}
