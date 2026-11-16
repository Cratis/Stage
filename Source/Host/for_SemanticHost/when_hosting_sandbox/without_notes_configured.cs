// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Showrunner.Notes;
using Cratis.Specifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost.when_hosting_sandbox;

public class without_notes_configured : when_hosting_published.given.a_semantic_host
{
    // Notes options alone, without the sandbox switch, change nothing.
    void Establish() => _arguments = ["--Notes:ApplicationName=Ledger", "--Notes:PublicationId=play-1"];

    async Task Because() => await BuildHost();

    [Fact] void should_not_map_the_model_references() => _patterns.ShouldNotContain("/stage/model-references");
    [Fact] void should_not_host_the_notes_backend() => _app.Services.GetService<IOptions<NotesOptions>>()?.Value.ApplicationName.Value.ShouldBeEmpty();
}
