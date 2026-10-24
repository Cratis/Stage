// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

// Arc 22.112.0 recovers the reads, but its recovered source does not compile back to an executable model yet
// (performer query, handler intents): Cratis/Stage#515.
[PerFactSpecificationLifecycle]
public class when_recovering_protected_command_reads : given.a_rendered_application
{
    const string Pending = "Cratis/Stage#515";

    protected override string Source => ProtectedCommandReadsCorpus.V25.SourceForms.Single(form => form.Name == "single").Documents.Single().Text;
    void Because() => Generate();

#pragma warning disable xUnit1004 // Preserve the four tracked Stage#515 skips; migration must not enable unsupported round trips.
    [Fact(Skip = Pending)] void should_recover_the_protected_read_mode() => _recovered.Application.CommandReadMode.ShouldEqual(SemanticCommandReadMode.Protected);
    [Fact(Skip = Pending)] void should_recover_the_read_and_its_key() => Recovered("Consume").Reads.Select(read => read.Name).ShouldEqual(["Current"]);
    [Fact(Skip = Pending)] void should_recover_the_read_backed_requirement() => Recovered("Consume").Requirements.Select(requirement => requirement.Message).ShouldEqual(["The stored value must not be empty."]);
    [Fact(Skip = Pending)] void should_recover_the_read_path_mapping() => Recovered("Consume").Produces.Single().Mappings.Single().Source.ShouldBeOfExactType<SemanticReadPathExpression>();
#pragma warning restore xUnit1004

    SemanticCommand Recovered(string name) => _recovered.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
        .SelectMany(slice => slice.Commands).Single(command => command.Name == name);
}
#endif
