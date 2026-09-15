// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

using NativeScreenplayCompiler = Cratis.Screenplay.ScreenplayCompiler;

namespace Cratis.Stage.Rendering.Cratis.for_QueryRenderer;

public class when_admitting_authored_query_intent
{
    [Fact]
    void should_reject_a_single_filter_before_mutating_any_owned_query_output()
    {
        var query = Compile("filter status String");
        RejectEveryShape(query, UnsupportedQueryIntentReason.FilterContract, query.Filters.Single().Location);
    }

    [Fact]
    void should_report_the_first_of_multiple_filters_even_with_a_performer()
    {
        var query = Compile("filter status String\nfilter owner String\nperformer\n  file ../QueryPerformers/Invoices.cs");
        query.Filters.Count().ShouldEqual(2);
        RejectEveryShape(query, UnsupportedQueryIntentReason.FilterContract, query.Filters.First().Location);
    }

    [Fact]
    void should_reject_a_symbolic_file_performer_for_every_query_shape()
    {
        var query = Compile("performer\n  file ../QueryPerformers/Invoices.cs");
        RejectEveryShape(query, UnsupportedQueryIntentReason.FilePerformer, query.Performer!.File!.Location);
    }

    [Fact]
    void should_reject_inline_behavior_without_parsing_or_exposing_it()
    {
        var query = Compile("performer\n  csharp\n    ```\n    return Array.Empty<InvoiceSummary>().AsQueryable();\n    ```");
        RejectEveryShape(query, UnsupportedQueryIntentReason.InlinePerformer, query.Performer!.Code!.Location);
    }

    [Fact]
    void should_fail_closed_for_a_performer_with_neither_attachment()
    {
        var query = Compile("performer\n  file Missing.cs");
        query = query with { Performer = query.Performer! with { File = null } };
        RejectEveryShape(query, UnsupportedQueryIntentReason.UnsupportedPerformer, query.Performer!.Location);
    }

    [Fact]
    void should_fail_closed_for_a_performer_with_both_attachments()
    {
        var query = Compile("performer\n  file Missing.cs");
        query = query with { Performer = query.Performer! with { Code = new CodeBlockSyntax("csharp", "not even C#", SourceLocation.Start) } };
        RejectEveryShape(query, UnsupportedQueryIntentReason.UnsupportedPerformer, query.Performer!.Location);
    }

    [Fact]
    void should_ignore_unsafe_queries_owned_by_another_model_and_preserve_the_fixed_pair()
    {
        var unsafeQuery = Compile("filter status String");
        var builder = new CSharpCodeBuilder();
        QueryRenderer.Render(builder, "AnotherModel", "Guid", "id", [unsafeQuery], new ApplicationSet([]), []);
        builder.ToString().ShouldContain("AllAnotherModels(");
        builder.ToString().ShouldContain("AnotherModelById(");
        builder.ToString().ShouldNotContain("Invoices(");
    }

    [Fact]
    void should_ignore_nonowned_unsafe_queries_beside_an_owned_supported_query()
    {
        var unsafeQuery = Compile("performer\n  file Missing.cs");
        var own = unsafeQuery with { Name = "Mine", ReturnType = unsafeQuery.ReturnType with { Name = "AnotherModel" }, Performer = null };
        var builder = new CSharpCodeBuilder();
        QueryRenderer.Render(builder, "AnotherModel", "Guid", "id", [unsafeQuery, own], new ApplicationSet([]), []);
        builder.ToString().ShouldContain("IQueryable<AnotherModel> Mine(");
        builder.ToString().ShouldNotContain("AnotherModelById(");
        builder.ToString().ShouldNotContain("Invoices(");
    }

    static void RejectEveryShape(QuerySyntax query, UnsupportedQueryIntentReason reason, SourceLocation location)
    {
        foreach (var observable in new[] { false, true })
        {
            foreach (var by in new[] { false, true })
            {
                var shaped = query with
                {
                    IsObservable = observable,
                    ReturnType = query.ReturnType with { IsCollection = !by },
                    By = by ? new QueryParameterSyntax("id", new TypeRefSyntax("Uuid", false, false, SourceLocation.Start), SourceLocation.Start) : null
                };
                var earlier = shaped with { Name = "EarlierSupported", Filters = [], Performer = null };
                var builder = new CSharpCodeBuilder().Using("Existing.Import").Namespace("Existing.Namespace").OpenBlock("public record Existing").Line("// retain these exact bytes");
                var before = Encoding.UTF8.GetBytes(builder.ToString());
                var diagnostics = new List<string> { "prior diagnostic" };
                var exception = Catch.Exception(() => QueryRenderer.Render(builder, "InvoiceSummary", "Guid", "id", [earlier, shaped], new ApplicationSet([]), diagnostics));
                var failure = Assert.IsType<UnsupportedQueryIntent>(exception);
                Assert.Equal(before, Encoding.UTF8.GetBytes(builder.ToString()));
                Assert.Equal(["prior diagnostic"], diagnostics);
                failure.QueryName.ShouldEqual(query.Name);
                failure.ReadModel.ShouldEqual(query.ReturnType.Name);
                failure.Reason.ShouldEqual(reason);
                failure.Location.ShouldEqual(location);
                failure.SlicePath.ShouldBeNull();
                failure.Message.ShouldContain("STAGE-CRATIS-QUERY-001");
                failure.Message.ShouldNotContain("Array.Empty");
                failure.Message.ShouldNotContain("not even C#");
            }
        }
    }

    static QuerySyntax Compile(string intent)
    {
        const string prefix = """
            module Billing
              feature Invoices
                slice StateView Summary
                  readmodel InvoiceSummary
                    status String
                  query Invoices => InvoiceSummary[]
            """;
        var source = prefix + "\n" + string.Join('\n', intent.Split('\n').Select(line => "        " + line));
        var result = new NativeScreenplayCompiler().Compile(source);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        result.Diagnostics.ShouldBeEmpty();
        return result.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single();
    }
}
