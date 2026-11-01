// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticTriggerArtifactRenderer;

public class when_rendering_a_trigger_that_shadows_a_framework_type : given.an_application_trigger_reaction
{
    readonly Dictionary<string, (bool Success, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings)> _results = [];

    void Because()
    {
        foreach (var triggerName in new[] { "IEventStore", "Task", "TriggerAttribute", "OccurrenceContext" })
        {
            SetContext(for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(Source.Replace("BatchRequested", triggerName, StringComparison.Ordinal)));
            Plan();
            var files = for_SemanticOccurrenceHandlerArtifactRenderer.when_rendering_application_trigger_productions.Files(_plan);
            _results[triggerName] = (_plan.Success, for_CratisRenderer.RenderedOutput.Errors(files), for_CratisRenderer.RenderedOutput.Warnings(files));
        }
    }

    [Theory]
    [InlineData("IEventStore")]
    [InlineData("Task")]
    [InlineData("TriggerAttribute")]
    [InlineData("OccurrenceContext")]
    public void should_compile_imports_for_a_shadowed_framework_type(string triggerName)
    {
        _results[triggerName].Success.ShouldBeTrue();
        _results[triggerName].Errors.ShouldBeEmpty();
        _results[triggerName].Warnings.ShouldBeEmpty();
    }
}
