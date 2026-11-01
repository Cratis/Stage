// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_releasing_application_bodies : Specification
{
    [Fact]
    async Task should_dispose_the_shared_worker_when_the_application_unloads()
    {
        var directory = Directory.CreateTempSubdirectory("stage-worker-lifetime-").FullName;
        try
        {
            var path = Path.Combine(directory, "Model.play");
            await File.WriteAllTextAsync(path, "concept Name : String");
            var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
            var worker = Substitute.For<IBodyWorkerClient, IAsyncDisposable>();
            var bodies = SemanticBodyExecution.From(loaded, new("revision", []), () => worker);
            var provider = await bodies.CreateAsync(loaded.Model.Application, ImmutableDictionary<string, SemanticImplementationBody>.Empty, default);
            await provider.DisposeAsync();
            await ((IAsyncDisposable)worker).DidNotReceive().DisposeAsync();
            var runtime = SemanticRuntimeHosting.Create(loaded.Plan, Substitute.For<IAppendSemanticFacts>(), SemanticWorld.Empty, bodies);
            ((IDisposable)runtime).Dispose();
            await ((IAsyncDisposable)worker).Received(1).DisposeAsync();
            await bodies.DisposeAsync();
            await ((IAsyncDisposable)worker).Received(1).DisposeAsync();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
