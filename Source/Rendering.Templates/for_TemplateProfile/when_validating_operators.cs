// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_validating_operators : Specification
{
    [Theory]
    [InlineData("{\"operators\":[],\"extra\":0}")]
    [InlineData("{\"operators\":[{\"operator\":\"+\"}]}")]
    [InlineData("{\"operators\":[],\"operators\":[]}")]
    [InlineData("{\"operators\":42}")]
    public void should_refuse_bad_table_shapes(string source) => ((TemplateRefusal)Catch.Exception(() => OperatorTable.Parse(Encoding.UTF8.GetBytes(source)))).Diagnostic.Code.ShouldEqual(TemplateCodes.OperatorTable);

    [Fact]
    public void should_refuse_duplicate_rows()
    {
        const string source = when_lowering.Operators;
        var row = source[14..^2].Split("},{", StringSplitOptions.None)[0] + "}";
        var duplicate = "{\"operators\":[" + row + "," + row + "]}";
        ((TemplateRefusal)Catch.Exception(() => OperatorTable.Parse(Encoding.UTF8.GetBytes(duplicate)))).Diagnostic.Code.ShouldEqual(TemplateCodes.OperatorTable);
    }
}
