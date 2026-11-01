// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_locating_errors_in_a_query_performer_body : given.a_source_plan
{
    LoadedSemanticModel _performers = null!;

    void Establish() => File.WriteAllText(Path.Combine(_root, "performers.play"), """
        module Projects
          feature Registration
            slice StateView ChangeRequests
              readmodel ChangeRequest
                talkId Uuid key
                requestedAt DateTime
              query AllChangeRequests => ChangeRequest[]
                performer
                  ```csharp
                  _ = 1;
                  return missing.OrderBy(request => request.RequestedAt);
                  ```
        """);

    async Task Because()
    {
        var loaded = await SemanticModelLoader.LoadAsync(_root, [], null, _planOptions.ApplicationName);
        Assert.True(loaded.Success, string.Join("; ", loaded.Diagnostics));
        _performers = loaded.Loaded!;
        _result = CratisRendering.PlanFrom(_performers, _featureSelection, _planOptions);
    }

    [Fact]
    void should_locate_the_error_in_the_authored_body()
    {
        var requirement = Assert.Single(_performers.ImplementationRequirements);
        var expected = $"{requirement.Source.Span.Document}:{requirement.BodyLines[1].Line}:{requirement.BodyLines[1].Column + 7}";
        Assert.Contains(_result.Diagnostics, diagnostic => diagnostic.Message.Contains("Query performer requirement", StringComparison.Ordinal) &&
            diagnostic.Message.Contains(expected, StringComparison.Ordinal) && diagnostic.Message.Contains("CS0103", StringComparison.Ordinal));
    }
}
