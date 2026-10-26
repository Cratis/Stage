// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_refusing_unprovable_generated_imports
{
    [Theory]
    [InlineData("public class Host : UnknownBase {}")]
    [InlineData("using Unknown; public class Host {}")]
    [InlineData("#if DEBUG\npublic class Host {}\n#endif")]
    [InlineData("public class Host {")]
    [InlineData("namespace Another; public class Host {}")]
    [InlineData("public class Host { public global::System.Unknown Value = null!; }")]
    [InlineData("public class Host { public global::System.Collections.Generic.Dictionary<int, string>.Unknown Value = null!; }")]
    [InlineData("public class Host { public global::Uncataloged.Unknown Value = null!; }")]
    void should_refuse_instead_of_emitting_unproved_source(string body)
    {
        var builder = new CSharpCodeBuilder(GeneratedProjects.Application, []).Namespace("Example").Line(body);
        var exception = Assert.Throws<CSharpImportRejected>(builder.ToString);
        Assert.NotEmpty(exception.Message);
    }
}
