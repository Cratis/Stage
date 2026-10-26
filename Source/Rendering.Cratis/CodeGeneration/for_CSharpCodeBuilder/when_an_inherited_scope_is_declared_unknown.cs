// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_an_inherited_scope_is_declared_unknown
{
    [Fact]
    void should_refuse_instead_of_emitting_qualified_source()
    {
        var builder = new CSharpCodeBuilder(new ImportRequest("Shop.Orders", GeneratedProjects.Application, [], []));
        builder.Inherits("Shop.External.Base");
        builder.Namespace("Shop.Orders").Line("public record Order(global::System.Text.RegularExpressions.Regex Pattern);");
        var failure = Assert.Throws<CSharpImportRejected>(builder.ToString);
        Assert.Contains(nameof(UnknownScopeReason.Declared), failure.Message);
    }
}
