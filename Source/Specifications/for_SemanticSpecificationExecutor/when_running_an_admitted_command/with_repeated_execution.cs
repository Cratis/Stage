// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_repeated_execution : a_command_only_plan
{
    Type _first = null!;
    Type _second = null!;
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        var executor = new SemanticSpecificationExecutor();
        var cache = (ConcurrentDictionary<SemanticRevision, SemanticRuntimeTypes>)typeof(SemanticSpecificationExecutor)
            .GetField("_types", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var selection = new SemanticSpecificationSelection([_specification.Id]);
        await executor.Run(_plan, selection, new());
        _first = cache[_plan.Revision].ForCommand(_plan.Commands[_specification.When!.Command]);
        var equivalentPlan = SemanticExecutionPlan.Compile(_plan.Model).Plan!;
        _report = await executor.Run(equivalentPlan, selection, new());
        _second = cache[equivalentPlan.Revision].ForCommand(equivalentPlan.Commands[_specification.When!.Command]);
    }

    [Fact] void should_reuse_the_command_runtime_type() => Xunit.Assert.Same(_first, _second);
    [Fact] void should_pass_on_repeated_run() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
}
