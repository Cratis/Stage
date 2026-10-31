// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticImplementationReach;

public class when_a_query_argument_has_code_validation : Specification
{
    [Fact]
    async Task should_refuse_with_the_named_not_scheduled_diagnostic()
    {
        const string source = """
            concept Name : String
              validate csharp
                ```csharp
                yield break;
                ```
            module Work
              feature Items
                slice StateView View
                  readmodel Snapshot
                    id Name key
                  query SnapshotById => Snapshot?
                    by id Name
            """;
        var root = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        var directory = Path.Combine(root, ".ai-work", "891", "specs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "model.play");
        await File.WriteAllTextAsync(path, source);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
        var refusal = SemanticImplementationReach.ForQuery(loaded.Plan, loaded.Plan.Queries.Values.Single());
        Assert.NotNull(refusal);
        refusal.Details.ShouldEqual("Code validations on query arguments are not executed by `cratis run` yet (Stage#891, not scheduled).");
    }
}
