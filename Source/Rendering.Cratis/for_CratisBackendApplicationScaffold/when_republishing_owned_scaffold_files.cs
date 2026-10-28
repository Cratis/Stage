// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_republishing_owned_scaffold_files : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_apply_the_callers_file_header_to_the_global_usings(bool header)
    {
        var options = new CratisPlanOptions("Shop", "Shop.Backend", "Acme.Shop") { FileHeader = header ? ["My application"] : [] };
        var artifact = CratisRendering.PlanScaffold(options).Artifacts.Single(candidate => candidate.RelativePath == "GlobalUsings.cs");
        Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).ShouldEqual(
            (header ? "// My application\n\n" : string.Empty) +
            "global using Cratis.Arc.MongoDB;\nglobal using Cratis.Chronicle.Keys;\nglobal using MongoDB.Driver;\nglobal using System.Reactive.Subjects;\n");
    }
}
