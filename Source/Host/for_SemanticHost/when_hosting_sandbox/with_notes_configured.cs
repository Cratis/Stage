// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Showrunner.Notes;
using Cratis.Specifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost.when_hosting_sandbox;

public class with_notes_configured : when_hosting_published.given.a_semantic_host
{
    void Establish() => _arguments =
    [
        "--Stage:Sandbox:Notes:Enabled=true",
        "--Notes:ApplicationName=Ledger",
        "--Notes:PublicationId=play-1",
        "--Notes:Transport=Http",
        "--Notes:CollectorUrl=https://collector.example.invalid",
        "--Notes:CollectorToken=session-token"
    ];

    async Task Because() => await BuildHost();

    [Fact] void should_admit_the_model() => _refused.ShouldBeFalse();
    [Fact] void should_map_the_model_references() => _patterns.ShouldContain("/stage/model-references");
    [Fact] void should_host_the_notes_backend() => _app.Services.GetRequiredService<IOptions<NotesOptions>>().Value.ApplicationName.Value.ShouldEqual("Ledger");
    [Fact] void should_keep_the_sandbox_tooling() => _patterns.ShouldContain("/stage/semantic/admission");
    [Fact] void should_keep_arc_discovery() => _patterns.Any(pattern => pattern.StartsWith("/.cratis/", StringComparison.Ordinal)).ShouldBeTrue();
}
