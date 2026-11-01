// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class with_a_user_modified_global_usings_republished_at_application_scope : given.a_republished_application_plan
{
    // GlobalUsings.cs is renderer-owned like Program.cs and the project file: the publication regenerates it.
    protected override string? Existing(string path, string planned) => path == GlobalUsingsPath
        ? planned.Replace("global using MongoDB.Driver;\n", string.Empty, StringComparison.Ordinal) + "global using My.Custom.Namespace;\n"
        : planned;

    [Fact] void should_be_compatible() => _check.ShouldBeOfExactType<CratisPublicationCheck.Compatible>();
    [Fact] void should_replace_the_modification_with_the_scaffold_bytes() => Encoding.UTF8.GetString(_full.Artifacts.Single(artifact => artifact.RelativePath == GlobalUsingsPath).Bytes.AsSpan())
        .ShouldEqual(Encoding.UTF8.GetString(ScaffoldBytes()));
    [Fact] void should_compile_with_only_the_planned_global_usings() => string.Join(Environment.NewLine, _errors).ShouldEqual(string.Empty);

    byte[] ScaffoldBytes() => CratisRendering.PlanScaffold(_options).Artifacts.Single(artifact => artifact.RelativePath == GlobalUsingsPath).Bytes.ToArray();
}
