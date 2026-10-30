// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_the_file_has_a_conditional_directive
{
    [Fact]
    void should_refuse_instead_of_guessing_the_other_preprocessor_branch()
    {
        var builder = new CSharpCodeBuilder(new ImportRequest("Shop.Common", GeneratedProjects.Application, [], []));
        builder.Namespace("Shop.Common").Line("#if DEBUG")
            .Line($"public record Item({builder.Type(new("System.Text.RegularExpressions", "Regex", 0, ReferenceContext.Type))} Pattern);")
            .Line("#endif");
        var failure = Assert.Throws<CSharpImportRejected>(builder.ToString);
        Assert.Contains(nameof(UnknownScopeReason.ConditionalDirective), failure.Message);
    }
}
