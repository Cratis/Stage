// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_a_base_class_is_not_known
{
    [Fact]
    void should_refuse_a_base_whose_members_cannot_be_proved()
    {
        var builder = new CSharpCodeBuilder(new ImportRequest("Shop.Common", GeneratedProjects.Application, [], []));
        builder.Namespace("Shop.Common").Line($"public class Item : UnknownBase {{ public {builder.Type(new("System.Text.RegularExpressions", "Regex", 0, ReferenceContext.Type))} Pattern {{ get; init; }} }}");
        var failure = Assert.Throws<CSharpImportRejected>(builder.ToString);
        Assert.Contains(nameof(UnknownScopeReason.UnknownBase), failure.Message);
    }
}
