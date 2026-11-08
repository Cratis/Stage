// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Publishing.for_StageApplicationExport;

public class when_exporting_a_model_with_named_sinks : given.an_exportable_model
{
    const string WithSinks = """
        sink Reporting for readmodels
        sink People for graphs
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
            slice StateView OrderLedger
              readmodel LedgerEntry
                sink Reporting
                id   Id key
                name String
              projection BuildLedger => LedgerEntry
                from OrderPlaced
                  id   = $eventSourceId
                  name = name
        """;

    StageApplicationPackage _package = null!;

    void Because() => _package = StageApplicationExport.Export(Request(WithSinks));

    [Fact] void should_list_the_declared_sinks_in_order() => _package.Manifest.Sinks.ShouldEqual(new PackageSink("Reporting", PackageSink.ReadModels), new PackageSink("People", PackageSink.Graphs));
    [Fact] void should_write_each_sink_with_its_kind() => _package.ManifestJson.ShouldContain("\"sinks\": [\n    {\n      \"name\": \"Reporting\",\n      \"kind\": \"readmodels\"\n    },\n    {\n      \"name\": \"People\",\n      \"kind\": \"graphs\"\n    }\n  ]");
    [Fact] void should_read_the_sinks_back() => PackageManifestJson.Read(_package.ManifestJson).Sinks.SequenceEqual(_package.Manifest.Sinks).ShouldBeTrue();
}
