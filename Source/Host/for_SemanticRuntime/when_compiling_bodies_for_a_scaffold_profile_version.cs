// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_compiling_bodies_for_a_scaffold_profile_version : Specification
{
    string _directory = null!;
    BodyUnitEntry _unset = null!;
    BodyUnitEntry _version3 = null!;
    BodyUnitEntry _version4 = null!;

    void Establish() => _directory = Directory.CreateTempSubdirectory("stage-host-profile-spec-").FullName;
    void Destroy() => Directory.Delete(_directory, true);

    async Task Because()
    {
        _unset = await CompileEntry(null);
        _version3 = await CompileEntry("3");
        _version4 = await CompileEntry("4");
    }

    [Fact] void should_admit_the_unset_version_body() => _unset.Admitted.ShouldBeTrue();
    [Fact] void should_admit_the_version_3_body() => _version3.Admitted.ShouldBeTrue();
    [Fact] void should_admit_the_version_4_body() => _version4.Admitted.ShouldBeTrue();
    [Fact] void should_compile_an_unset_version_as_version_3() => _unset.UnitHash.ShouldEqual(_version3.UnitHash);
    [Fact] void should_compile_version_4_for_a_different_framework_than_version_3() => _version4.UnitHash.ShouldNotEqual(_version3.UnitHash);

    async Task<BodyUnitEntry> CompileEntry(string? profileVersion)
    {
        var path = Path.Combine(_directory, "Model.play");
        await File.WriteAllTextAsync(path, Model);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
        var notes = new List<SemanticModelLoadDiagnostic>();
        var settings = new SemanticBodyWorkerOptions(true, Path.Combine(AppContext.BaseDirectory, "bodyworker", "Cratis.Stage.BodyWorker.dll"))
        {
            CacheDirectory = Path.Combine(_directory, "cache", profileVersion ?? "unset"),
            CallTimeoutMilliseconds = 10000,
            ProfileVersion = profileVersion
        };
        await using var bodies = await SemanticBodyWorkerHosting.Start(loaded, path, settings, notes);
        Assert.True(bodies is not null, string.Join(';', notes.Select(note => note.Message)));
        return bodies.Manifest.Entries.Single();
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
