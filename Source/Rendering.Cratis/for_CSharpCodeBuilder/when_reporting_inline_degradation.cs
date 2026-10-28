// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.CodeGeneration.for_InlineCode.when_verifying_original_bytes;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_reporting_inline_degradation : Specification
{
    [Fact]
    void should_report_the_warning_before_emitting_a_degraded_body()
    {
        var diagnostics = new List<InlineCodeDiagnostic>();
        var builder = new CSharpCodeBuilder(GeneratedProjects.Application, [], true).Namespace("Shop").OpenBlock("class Host").OpenBlock("void Run()");
        builder.Inline(Code(" ", "java"), Site, reportDegradation: diagnostics.Add);
        var output = builder.EndBlock().EndBlock().ToString();
        Assert.Contains("throw new NotSupportedException", output);
        Assert.DoesNotContain("global::System.NotSupportedException", output);
        Assert.Equal("STAGE-DEGRADE-003", Assert.Single(diagnostics).Code);
    }

    [Fact]
    void should_qualify_a_placeholder_only_when_its_type_name_collides()
    {
        var builder = new CSharpCodeBuilder(GeneratedProjects.Application, [], true).Namespace("Shop").Line("class NotSupportedException {}").OpenBlock("class Host").OpenBlock("void Run()");
        builder.Inline(Code(" ", "java"), Site, reportDegradation: _ => { });
        var output = builder.EndBlock().EndBlock().ToString();
        Assert.Contains("using StageImport0 = global::System;", output);
        Assert.Contains("throw new StageImport0::NotSupportedException", output);
        Assert.DoesNotContain("throw new global::", output);
    }

    [Fact]
    void should_refuse_to_silently_discard_a_degradation_warning()
    {
        var builder = new CSharpCodeBuilder().Inline(Code(" ", "java"), Site);
        var error = Assert.Throws<InlineCodeRejected>(builder.ToString);
        Assert.Equal("STAGE-DEGRADE-003", Assert.Single(error.Diagnostics).Code);
    }
}
