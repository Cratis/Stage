// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_executing_reducer_bodies_with_concepts : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_construct_a_model_concept_in_the_worker(bool file)
    {
        var directory = Directory.CreateTempSubdirectory("stage-reducer-concepts-").FullName;
        try
        {
            const string body = "return new TalkScore(TalkId: new TalkId(Guid.Parse(context.Key)), Score: (context.State?.Score ?? 0) + context.Event.Score, Note: null);";
            var model = when_executing_reducer_bodies.Model.Replace(
                "return new TalkScore(TalkId: Guid.Parse(context.Key), Score: (context.State?.Score ?? 0) + context.Event.Score, Note: null);",
                body,
                StringComparison.Ordinal);
            if (file)
            {
                await File.WriteAllTextAsync(Path.Combine(directory, "Reduce.cs"), body);
                model = model.Replace($"```csharp\n          {body}\n          ```", "file Reduce.cs", StringComparison.Ordinal);
            }
            var path = Path.Combine(directory, "eventmodel.play");
            await File.WriteAllTextAsync(path, model);
            var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
            var notes = new List<SemanticModelLoadDiagnostic>();
            await using var bodies = await SemanticBodyWorkerHosting.Start(
                loaded,
                path,
                new(true, Path.Combine(AppContext.BaseDirectory, "bodyworker", "Cratis.Stage.BodyWorker.dll"))
                {
                    CacheDirectory = Path.Combine(directory, "cache"),
                    CallTimeoutMilliseconds = 30000
                },
                notes);
            Assert.True(bodies is not null, string.Join(';', notes.Select(note => note.Message)));
            Assert.All(bodies.Manifest.Entries, entry => Assert.True(entry.Admitted, entry.DiagnosticMessage));
            var (runtime, _) = when_executing_reducer_bodies.Runtime(loaded, bodies);
            using var lifetime = (IDisposable)runtime;
            Assert.IsType<SemanticAccepted>(await when_executing_reducer_bodies.Execute(runtime, loaded, "ScoreTalk", ("score", 7)));
            var readModel = loaded.Plan.ReadModels.Values.Single();
            var row = Assert.Single(await runtime.ReadModels(readModel.Id)).Values;
            var score = readModel.Properties.Single(property => property.Name == "score");
            row.Single(value => value.TargetProperty == score.Id).Value.ShouldEqual(SemanticValue.Number(7));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
