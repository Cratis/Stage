// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost.when_hosting_published;

public class with_a_refused_model : given.a_semantic_host
{
    async Task Establish()
    {
        _arguments = ["--Stage:Mode=Published", "--Stage:Sandbox:ForwardedIdentity=true"];
        await File.WriteAllTextAsync(_path, "not a Screenplay model");
    }

    async Task Because() => await BuildHost();

    [Fact] void should_refuse_the_model() => _refused.ShouldBeTrue();
    [Fact] void should_not_restore_tooling_on_refusal() => _patterns.Where(pattern => pattern.StartsWith("/stage/", StringComparison.Ordinal)).ShouldContainOnly(["/stage/status", "/stage/packages"]);
    [Fact] void should_not_map_the_read_model_page() => _patterns.ShouldNotContain("/read-models");
    [Fact] void should_not_redirect_to_the_read_model_page() => _readModelPage.Response.StatusCode.ShouldEqual(StatusCodes.Status404NotFound);
    [Fact] void should_keep_the_spa_fallback() => _patterns.ShouldContain("{*path:nonfile}");
}
