// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_authored_text_contains_template_markers : Specification
{
    const string AuthoredSource = """
        public static class Authored
        {
            public const string Template = "{{model.name}}";
            public const string Include = "{{include fragment}}";
            public const string Token = "__STAGE_TOKEN_0__";
            public const string Repeated = "{{model.name}} {{model.name}} __STAGE_TOKEN_0__ __STAGE_TOKEN_0__";
            public static global::System.Text.RegularExpressions.Regex Pattern() => new("{{include fragment}}");
        }
        """;

    const string Expected = """
        using System.Text.RegularExpressions;

        namespace Shop.Common;

        public record Item(Regex Pattern);
        public static class Authored
        {
            public const string Template = "{{model.name}}";
            public const string Include = "{{include fragment}}";
            public const string Token = "__STAGE_TOKEN_0__";
            public const string Repeated = "{{model.name}} {{model.name}} __STAGE_TOKEN_0__ __STAGE_TOKEN_0__";
            public static global::System.Text.RegularExpressions.Regex Pattern() => new("{{include fragment}}");
        }
        """ + "\n";

    CSharpCodeBuilder _rawBuilder = null!;
    CSharpCodeBuilder _verbatimBuilder = null!;
    string _rawResult = null!;
    string _verbatimResult = null!;

    void Establish()
    {
        _rawBuilder = Builder().Raw(AuthoredSource);
        _verbatimBuilder = Builder().RawVerbatim(AuthoredSource);
    }

    void Because()
    {
        _rawResult = _rawBuilder.ToString();
        _verbatimResult = _verbatimBuilder.ToString();
    }

    [Fact] void should_preserve_raw_marker_data_and_only_shorten_the_generated_reference() => _rawResult.ShouldEqual(Expected);
    [Fact] void should_preserve_verbatim_marker_data_and_only_shorten_the_generated_reference() => _verbatimResult.ShouldEqual(Expected);

    static CSharpCodeBuilder Builder()
    {
        var builder = new CSharpCodeBuilder(new ImportRequest("Shop.Common", GeneratedProjects.Application, [], []));
        return builder.Namespace("Shop.Common")
            .Line($"public record Item({builder.Type(new("System.Text.RegularExpressions", "Regex", 0, ReferenceContext.Type))} Pattern);");
    }
}
