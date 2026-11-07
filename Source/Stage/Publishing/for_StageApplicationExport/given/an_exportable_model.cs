// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Publishing.for_StageApplicationExport.given;

public class an_exportable_model : Specification
{
    protected const string Secured = """
        concept Id : Uuid
        policy Members
          require role "Member"
        module Shop
          feature Orders
            slice StateChange PlaceOrder
              command PlaceOrder
                id Id generated identifier
                name String
                authorize Members
                produces event OrderPlaced
                  name String = name
                returns id
        """;

    protected const string Open = """
        concept Id : Uuid
        module Shop
          feature Orders
            slice StateChange PlaceOrder
              command PlaceOrder
                id Id generated identifier
                name String
                produces event OrderPlaced
                  name String = name
                returns id
        """;

    protected StageApplicationExportRequest Request(string source = Secured) =>
        new(new Dictionary<string, string> { ["shop.play"] = source }, "Shop", "1.2.0", "my-shop");
}
