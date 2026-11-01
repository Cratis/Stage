// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Host.Export.for_StageExport.given;

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

    protected string _root = null!;
    protected string _model = null!;

    protected string Output(string name) => Path.Combine(_root, name);

    protected StageExportRequest Request(string output) => new(_model, "1.2.0", "my-shop", output);

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("stage-export-spec").FullName;
        _model = Path.Combine(_root, "source");
        Directory.CreateDirectory(_model);
        File.WriteAllText(Path.Combine(_model, "shop.play"), Secured);
    }

    void Destroy() => Directory.Delete(_root, recursive: true);
}
