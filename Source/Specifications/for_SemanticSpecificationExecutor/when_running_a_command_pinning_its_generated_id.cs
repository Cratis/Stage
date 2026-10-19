// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_a_command_pinning_its_generated_id : Specification
{
    SemanticSpecificationRunReport _report = null!;
    string _workspace = null!;

    async Task Because()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "cratis-stage-specs", "pinned-ids", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        var path = Path.Combine(_workspace, "Pinned.play");
        await File.WriteAllTextAsync(path, """
            concept OfferId : Uuid
              generate uuid
            module Work
              feature Selling
                slice StateChange Offering
                  command Offer
                    offerId OfferId generated identifier
                    sku String
                    produces Offered
                      for offerId
                      offerId = offerId
                      sku = sku
                  event Offered
                    offerId OfferId
                    sku String
                  specification OfferingAProduct
                    given generated Offer.offerId = "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01"
                    when Offer
                      sku = "mug"
                    then Offered
                      for "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01"
                      offerId = "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01"
                      sku = "mug"
            """);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
        _report = await new SemanticSpecificationExecutor().Run(loaded.Plan, new([]), new());
    }

    void Destroy() => Directory.Delete(_workspace, recursive: true);

    [Fact] void should_run_the_specification() => _report.Results.Count.ShouldEqual(1);
    [Fact] void should_use_the_pinned_value_as_the_generated_id() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
}
