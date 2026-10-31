// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor;

public class when_reusing_a_worker_across_providers : Specification
{
    [Fact]
    async Task should_release_providers_without_restarting_the_application_worker()
    {
        using var fixture = new a_real_worker();
        var path = Path.Combine(fixture.CacheDirectory, "Model.play");
        await File.WriteAllTextAsync(path, validation_body_model.Command("return context.Value > 0;"));
        await using var worker = fixture.Create();
        var manifest = await worker.CompileAsync(path);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
        var created = 0;
        var execution = SemanticBodyExecution.From(loaded, manifest, () =>
        {
            created++;
            return worker;
        });
        var body = Assert.Single(execution.Bodies);
        var context = new SemanticRuleContext(SemanticValue.Null, SemanticValue.Number(1), "amount", null, null, null);
        var first = await execution.CreateAsync(loaded.Model.Application, ImmutableDictionary<string, SemanticImplementationBody>.Empty, default);
        var second = await execution.CreateAsync(loaded.Model.Application, ImmutableDictionary<string, SemanticImplementationBody>.Empty, default);
        var answers = await Task.WhenAll(first.EvaluateAsync(body, context, default).AsTask(), second.EvaluateAsync(body, context, default).AsTask());
        answers.ShouldEachConformTo(answer => answer.Satisfied == true);
        var pid = worker.ProcessId;
        pid.ShouldNotBeNull();
        await first.DisposeAsync();
        (await second.EvaluateAsync(body, context, default)).Satisfied.ShouldEqual(true);
        worker.ProcessId.ShouldEqual(pid);
        created.ShouldEqual(1);
        await second.DisposeAsync();

        // The application owner, not either provider, releases the process.
        await execution.DisposeAsync();
        worker.ProcessId.ShouldBeNull();
        Assert.Throws<ArgumentException>(() => Process.GetProcessById(pid.Value));
    }
}
#endif
