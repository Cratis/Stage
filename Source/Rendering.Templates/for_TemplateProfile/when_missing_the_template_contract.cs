// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_missing_the_template_contract : for_TemplatePreparer.given.a_kit
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(true, 2)]
    public void should_refuse_a_kit_without_templates_v1(bool declared, int major)
    {
        var kit = Kit();
        kit = kit with { Manifest = kit.Manifest with { Requires = declared ? [new("templates", major, 0)] : [] } };
        var result = TemplateProfile.Prepare(kit, new given.a_language());
        result.Templates.ShouldBeNull();
        result.Diagnostics.Single().Code.ShouldEqual(TemplateCodes.MissingContract);
        result.Diagnostics.Single().Message.ShouldEqual("Kit must require templates/v1.x.");
    }
}
