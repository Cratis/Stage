// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_ReadModelConverter;

public class when_converting_a_passive_read_model : Specification
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_preserve_the_explicit_activation_choice(bool passive)
    {
        var source = """
            module Stock
              feature Decisions
                slice StateView Balance
                  readmodel Balance
                    PASSIVE
                    id String key
                    quantity Int
                  projection BuildBalance => Balance
                    from Adjusted
                      id = $eventSourceId
                      quantity = quantity
            """.Replace("PASSIVE", passive ? "passive" : string.Empty, StringComparison.Ordinal);
        var model = EventModelLoader.LoadFromSource(source);
        var projection = model.Collections.Single().Modules.Single().Features.Single().Slices.Single().ReadModel.Projection;
        projection.IsActive.ShouldEqual(!passive);
    }
}
#endif
