// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Publishing.for_PackageManifestJson;

public class when_reading_a_manifest_written_before_sinks : Specification
{
    const string Json = """
        {
          "schemaVersion": 1,
          "applicationName": "Shop",
          "publishedName": "my-shop",
          "applicationVersion": "1.2.0",
          "stageRuntimeVersion": "4.0.0",
          "publicationId": "pub-1",
          "modelDigest": "sha256:0",
          "notesEnabled": false,
          "authentication": { "required": true, "policies": [], "roles": [], "claims": [], "opaquePolicies": false },
          "modelReferences": []
        }
        """;

    PackageManifest _manifest = null!;

    void Because() => _manifest = PackageManifestJson.Read(Json);

    [Fact] void should_read_the_manifest() => _manifest.ApplicationName.ShouldEqual("Shop");
    [Fact] void should_list_no_sinks() => _manifest.Sinks.ShouldBeEmpty();
}
