// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_admitting_the_profile;

public class and_the_kit_is_admitted : given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;
    System.Collections.Immutable.ImmutableArray<byte> _kit;

    void Establish() => _kit = Archive();

    void Because() => _plan = Plan(Profile(_kit));

    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _plan.Diagnostics.ShouldBeEmpty();
    [Fact] void should_record_the_kit_with_its_digest() => _plan.InputProvenance.Components.ShouldContain(new ArtifactRenderComponent(ArtifactRenderComponentKind.Kit, "acme", "1.0.0", Rendering.Kits.KitDigest.Compute(_kit)));
    [Fact] void should_record_the_language_pack() => _plan.InputProvenance.Components.ShouldContain(new ArtifactRenderComponent(ArtifactRenderComponentKind.LanguagePack, "test", "1.0.0"));
    [Fact] void should_record_the_template_profile() => _plan.InputProvenance.Components.ShouldContain(new ArtifactRenderComponent(ArtifactRenderComponentKind.TemplateProfile, "stage-templates/v1", "templates/v1.0"));
    [Fact] void should_record_the_helper_catalogue_digest() => _plan.InputProvenance.Components.Single(_ => _.Kind == ArtifactRenderComponentKind.HelperCatalogue).Digest!.Length.ShouldEqual(64);
    [Fact] void should_record_the_engine() => _plan.InputProvenance.Components.ShouldContain(new ArtifactRenderComponent(ArtifactRenderComponentKind.TemplateEngine, "scriban", "7.5.3"));
    [Fact] void should_record_exactly_the_five_components() => _plan.InputProvenance.Components.Length.ShouldEqual(5);
    [Fact] void should_pin_the_engine_to_the_central_package_version() => File.ReadAllText(PackagesFile()).ShouldContain("<PackageVersion Include=\"Scriban\" Version=\"[7.5.3]\" />");

    static string PackagesFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
        {
            directory = directory.Parent!;
        }

        return Path.Combine(directory.FullName, "Directory.Packages.props");
    }
}
