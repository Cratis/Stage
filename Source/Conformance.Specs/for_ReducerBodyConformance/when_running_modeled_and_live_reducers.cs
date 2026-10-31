// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host;
using Cratis.Stage.Host.for_SemanticRuntime;
using Cratis.Stage.Semantics;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ReducerBodyConformance;

public class when_running_modeled_and_live_reducers : Specification
{
    [Fact]
    async Task should_agree_between_the_modeled_specifications_and_live_execution()
    {
        var root = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        var worker = Path.Combine(root, "Source", "Host", "bin", "Debug", "net10.0", "bodyworker", "Cratis.Stage.BodyWorker.dll");
        Assert.True(File.Exists(worker), "Build Host in Debug before running the real-worker specifications.");
        var directory = Directory.CreateTempSubdirectory("stage-reducer-conformance-").FullName;
        try
        {
            var path = Path.Combine(directory, "Model.play");
            await File.WriteAllTextAsync(path, when_executing_reducer_bodies.Model);
            var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
            var notes = new List<SemanticModelLoadDiagnostic>();
            await using var bodies = await SemanticBodyWorkerHosting.Start(
                loaded,
                path,
                new(true, worker) { CacheDirectory = Path.Combine(directory, "cache"), CallTimeoutMilliseconds = 30000 },
                notes);
            Assert.True(bodies is not null, string.Join(';', notes.Select(note => note.Message)));

            var report = await new SemanticSpecificationExecutor().Run(loaded.Plan, new([.. loaded.Plan.Specifications.Keys]), new() { BodyExecution = bodies });
            Assert.Equal(2, report.Results.Count);
            Assert.All(report.Results, result => Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Name}: {result.Unsupported?.Details}{string.Join(';', result.Failures)} {string.Join(';', result.Trace?.ReadModels.Select(pair => pair.Key + "=" + pair.Value) ?? [])}"));

            var (runtime, _) = when_executing_reducer_bodies.Runtime(loaded, bodies);
            using var lifetime = (IDisposable)runtime;
            Assert.IsType<SemanticAccepted>(await when_executing_reducer_bodies.Execute(runtime, loaded, "ScoreTalk", ("score", 7)));
            Assert.IsType<SemanticAccepted>(await when_executing_reducer_bodies.Execute(runtime, loaded, "ScoreTalk", ("score", 5)));
            var readModel = loaded.Plan.ReadModels.Values.Single();
            var score = readModel.Properties.Single(property => property.Name == "score").Id;
            Assert.Single(await runtime.ReadModels(readModel.Id)).Values.Single(value => value.TargetProperty == score).Value.ShouldEqual(SemanticValue.Number(12));
            Assert.IsType<SemanticAccepted>(await when_executing_reducer_bodies.Execute(runtime, loaded, "WithdrawTalk", ("reason", "withdrawn")));
            Assert.Empty(await runtime.ReadModels(readModel.Id));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
#endif
