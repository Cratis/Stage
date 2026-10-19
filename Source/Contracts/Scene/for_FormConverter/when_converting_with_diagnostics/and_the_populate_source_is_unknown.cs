// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_FormConverter.when_converting_with_diagnostics;

public class and_the_populate_source_is_unknown : Specification
{
    [Fact]
    public void should_keep_the_form_and_report_the_omitted_source()
    {
        var location = new SourceLocation(6, 3, "forms.play");
        var form = new FormSyntax("Register", "RegisterProject", new UnknownPopulate(location), [], null, location);
        var diagnostics = new ConversionDiagnostics();
        var result = FormConverter.Convert(form, diagnostics, "Projects.Register");

        result.Name.ShouldEqual("Register");
        result.PopulateSource.ShouldBeNull();
        diagnostics.All.Single().Code.ShouldEqual("STAGE-SCENE-FORM-001");
        diagnostics.All.Single().Location.ShouldEqual(location);
        diagnostics.All.Single().Path.ShouldEqual("Projects.Register");
        Catch.Exception(() => FormConverter.Convert(form)).ShouldBeOfExactType<UnknownFormPopulateSource>();
    }

    sealed record UnknownPopulate(SourceLocation Location) : FormPopulateSource(Location);
}
