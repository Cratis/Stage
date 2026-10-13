// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_routes;

public class and_names_use_the_code_fallback : a_routed_command
{
    [Theory]
    [InlineData("class")]
    [InlineData("required")]
    [InlineData("notes")]
    [InlineData("Notes-Archive")]
    [InlineData("1Notes")]
    [InlineData("Account_Name")]
    public void should_admit_non_identity_stream_names(string name)
    {
        _source = _source with { Streams = [_source.Streams[0] with { Name = name }] };
        ReplaceSlice();
        Assert.True(invoice_model.Plan(_model).Success);
    }

    [Theory]
    [InlineData("account", "AccountEventSource")]
    [InlineData("Account_Name", "AccountNameEventSource")]
    [InlineData("accountEventSource", "AccountEventSource")]
    [InlineData("Account_EventSource", "AccountEventSource")]
    public void should_preserve_non_identity_source_class_names(string name, string className)
    {
        _source = _source with { Name = name };
        Plan();
        Assert.Contains(_plan.Artifacts, artifact => artifact.RelativePath == $"EventSources/{className}.cs");
    }
}
