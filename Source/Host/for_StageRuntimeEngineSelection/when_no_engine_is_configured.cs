// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageRuntimeEngineSelection;

[Collection(nameof(for_WarmStageHandoff.given.runtime_engine_environment))]
public class when_no_engine_is_configured : Specification
{
    string? _previousEngine;
    StageRuntimeEngine _selected;

    void Establish()
    {
        _previousEngine = Environment.GetEnvironmentVariable("Stage__Runtime__Engine");
        Environment.SetEnvironmentVariable("Stage__Runtime__Engine", null);
    }

    void Because() => _selected = StageRuntimeEngineSelection.Read([]);

    [Fact] void should_select_the_semantic_engine() => _selected.ShouldEqual(StageRuntimeEngine.Semantic);

    void Destroy() => Environment.SetEnvironmentVariable("Stage__Runtime__Engine", _previousEngine);
}
