// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Arc.Commands;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_recovering_protected_command_reads : given.a_rendered_application
{
    protected override string Source => ProtectedCommandReadsCorpus.V25.SourceForms.Single(form => form.Name == "single").Documents.Single().Text;
    void Because() => GenerateAuthoring();

    [Fact]
    void should_carry_project_read_protection_into_the_compilation()
    {
        var attribute = Assert.Single(_compilation.Assembly.GetAttributes(), attribute => attribute.AttributeClass?.ToDisplayString() == "Cratis.Arc.Commands.ReadProtectionAttribute");
        Assert.Single(attribute.ConstructorArguments).Value.ShouldEqual((int)ReadProtectionMode.On);
    }

    [Fact] void should_recover_the_protected_read_mode() => _recoveredAuthoring.CommandReadMode.ShouldEqual(CommandReadMode.Protected);
    [Fact] void should_recover_the_read_and_its_key() => Recovered("Consume").Reads.Select(read => (read.ReadModel, read.By)).ShouldEqual([("Current", "sourceId")]);
    [Fact] void should_recover_the_read_backed_requirement() => Recovered("Consume").Validations.OfType<DeclarativeValidateSyntax>().SelectMany(validation => validation.Requirements!).Select(requirement => requirement.Message).ShouldEqual(["The stored value must not be empty."]);
    [Fact] void should_recover_the_read_path_mapping() => ((PathExpressionSyntax)Recovered("Consume").Produces.Single().Mappings.Single().Source).Path.ShouldEqual("Current.value");

    static IEnumerable<SliceSyntax> Slices(IEnumerable<FeatureSyntax> features) =>
        features.SelectMany(feature => feature.Slices.Concat(Slices(feature.Features)));

    CommandSyntax Recovered(string name) => _recoveredAuthoring.Modules.SelectMany(module => Slices(module.Features))
        .SelectMany(slice => slice.Commands).Single(command => command.Name == name);
}
#endif
