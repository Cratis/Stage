// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaBindingVectors;

public class when_emitting_the_binding_vectors(BindingVectorOutput output) : Specification, IClassFixture<BindingVectorOutput>
{
    public const string Variable = "STAGE_JAVA_BINDING_VECTORS";
    string _root = null!;

    void Establish() => _root = output.Root;
    void Because() => output.Emit();

    [Fact] void should_emit_every_catalog_case() => Directory.GetDirectories(Path.Combine(_root, "positive")).Length.ShouldEqual(10);
    [Fact] void should_resolve_every_compiled_file_without_refusal() => JavaBindingVectors.Positive().All(item =>
        new JavaLanguagePack(JavaTypeFacts.Parse(item.FactsJson)).ResolveWithSites(item.Files).Where((_, index) => !item.ExcludedPaths.Contains(item.Files[index].Path)).All(file => file.Resolution.Refusal is null)).ShouldBeTrue();
    [Fact] void should_report_at_least_one_reference_site_per_case() => Directory.GetFiles(_root, "sites.tsv", SearchOption.AllDirectories).All(path => File.ReadAllLines(path).Length > 0).ShouldBeTrue();

    [Fact]
    void should_match_the_pinned_goldens_for_the_k1_cases()
    {
        foreach (var (path, golden) in new[] { ("collision/Collision", "collision"), ("inheritance/Child", "inheritance"), ("nesting/Outer", "nesting"), ("obscured/Child", "obscured") })
        {
            var actual = File.ReadAllText(Path.Combine(_root, "positive/k1-golden-cases/src/vectors/" + path + ".java"));
            actual.ShouldEqual(K1Fixture.Read("case-" + golden + ".java"));
        }
    }

    [Fact]
    void should_write_an_index_whose_counts_equal_the_sites_written()
    {
        foreach (var set in new[] { "positive", "negative" })
        {
            var root = Path.Combine(_root, set);
            var lines = File.ReadAllLines(Path.Combine(root, "index.tsv"));
            var files = Directory.GetFiles(root, "*.java", SearchOption.AllDirectories).Length;
            var sites = Directory.GetFiles(root, "sites.tsv", SearchOption.AllDirectories).Sum(path => File.ReadAllLines(path).Length);
            lines[0].ShouldEqual($"# java-binding-vectors v1 cases={lines.Length - 1} files={files} sites={sites}");
            foreach (var row in lines.Skip(1).Select(line => line.Split('\t')))
            {
                Directory.GetFiles(Path.Combine(root, row[0], "src"), "*.java", SearchOption.AllDirectories).Length.ShouldEqual(int.Parse(row[1], System.Globalization.CultureInfo.InvariantCulture));
                File.ReadAllLines(Path.Combine(root, row[0], "sites.tsv")).Length.ShouldEqual(int.Parse(row[2], System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }

    [Fact]
    void should_plant_exactly_one_negative_mismatch()
    {
        var root = Path.Combine(_root, "negative/under-included-facts");
        var lines = File.ReadAllLines(Path.Combine(root, "expected.tsv"));
        lines.Length.ShouldEqual(1);
        var row = lines[0].Split('\t');
        var text = File.ReadAllText(Path.Combine(root, "src", row[0]));
        text[int.Parse(row[1], System.Globalization.CultureInfo.InvariantCulture)..int.Parse(row[2], System.Globalization.CultureInfo.InvariantCulture)].ShouldEqual("String");
        row[3].ShouldEqual("java.lang.String");
        row[4].ShouldEqual("lib.Base.String");
    }

    [Fact]
    void should_generate_stubs_only_for_non_jdk_fact_types()
    {
        var stubs = JavaFactStubs.Generate("""{"types":{"java.lang.String":[],"javax.test.Type":[],"lib.Base":["String"]}}""").ToArray();
        stubs.Select(stub => stub.Path).ShouldEqual(["lib/Base.java"]);
        stubs[0].Text.Contains("public static class String {}", StringComparison.Ordinal).ShouldBeTrue();
        stubs[0].Text.Contains("public static int String;", StringComparison.Ordinal).ShouldBeTrue();
    }
}
