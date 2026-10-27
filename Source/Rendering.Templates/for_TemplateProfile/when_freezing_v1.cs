// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_freezing_v1 : for_TemplatePreparer.given.a_kit
{
    [Fact]
    public void should_freeze_the_complete_api_helpers_builtins_and_defaults()
    {
        using var reader = new StreamReader(typeof(TemplateProfile).Assembly.GetManifestResourceStream("Cratis.Stage.Rendering.Templates.v1.surface.sha256") ?? throw new InvalidOperationException("Missing surface digest."));
        TemplateSurface.Digest.ShouldEqual(reader.ReadToEnd().Trim());
    }
}
