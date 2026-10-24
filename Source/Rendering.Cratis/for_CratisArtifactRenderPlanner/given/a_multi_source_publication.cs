// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_multi_source_publication : a_translation_source
{
    const string Source = """
        module Shop
          feature Sales
            slice StateChange Orders
              event Drafted
                name String
              event AmountSet
                amount Int
              command Draft
                id String identifier
                name String
                produces Drafted
                  for id
                  name = name
              command SetAmount
                id String identifier
                amount Int
                produces AmountSet
                  for id
                  amount = amount
            slice Translate Publish
              direction outbound
              public event OrderPublished
                name String
                amount Int
              projection OrderPublisher => OrderPublished
                from Drafted
                  name = name
                from AmountSet
                  amount = amount
        """;

    void Establish() => _model = Compile(Source);
}
#endif
