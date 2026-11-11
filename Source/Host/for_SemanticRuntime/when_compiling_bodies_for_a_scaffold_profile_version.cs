// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_compiling_bodies_for_a_scaffold_profile_version : Specification
{
    [Fact]
    async Task should_compile_a_version_4_model_through_the_worker()
    {
        var (bodies, notes) = await Start("4");
        Assert.True(bodies is not null, string.Join(';', notes.Select(note => note.Message)));
    }

    [Fact]
    async Task should_fail_the_worker_for_an_unknown_profile_version()
    {
        var (bodies, notes) = await Start("99");
        bodies.ShouldBeNull();
        Assert.Single(notes).Code.ShouldEqual(BodyDiagnostics.WorkerFailed);
    }

    static async Task<(SemanticBodyExecution? Bodies, List<SemanticModelLoadDiagnostic> Notes)> Start(string profileVersion)
    {
        var directory = Directory.CreateTempSubdirectory("stage-host-profile-spec-").FullName;
        try
        {
            var path = Path.Combine(directory, "Model.play");
            await File.WriteAllTextAsync(path, Model);
            var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
            var notes = new List<SemanticModelLoadDiagnostic>();
            var settings = new SemanticBodyWorkerOptions(true, Path.Combine(AppContext.BaseDirectory, "bodyworker", "Cratis.Stage.BodyWorker.dll"))
            {
                CacheDirectory = Path.Combine(directory, "cache"),
                CallTimeoutMilliseconds = 10000,
                ProfileVersion = profileVersion
            };
            return (await SemanticBodyWorkerHosting.Start(loaded, path, settings, notes), notes);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    const string Model = """
        module M
          feature F
            slice StateChange Add
              command Add
                id String identifier
                amount Int
                validate
                  amount rule Positive message "Must be positive"
                    ```csharp
                    return context.Value > 0;
                    ```
                produces Added
                  for id
                  amount = amount
              event Added
                amount Int
        """;
}
