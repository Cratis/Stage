// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

[Collection("SpecRunner application boundary culture")]
public class when_running_semantic_engine : Specification
{
    readonly string _folder = Path.Combine(Path.GetTempPath(), $"stage semantic runner {Guid.NewGuid():N}");
    SemanticSpecificationRunReport _report = null!;
    int _exitCode;
    int _mixedCaseExit;
    int _missingCatalogExit;
    int _invalidCatalogExit;
    int _structuralFlagExit;
    string _missingCatalogMessage = string.Empty;
    string _invalidCatalogMessage = string.Empty;
    string _structuralFlagMessage = string.Empty;

    void Establish()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "Projects.play"),
            """
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    name String
                    validate
                      name not empty message "Project name is required"
                    produces ProjectRegistered
                      name = name
                  event ProjectRegistered
                    name String
                  specification RejectingEmptyName
                    when RegisterProject
                      name = ""
                    then error "Project name is required"
            """);
    }

    async Task Because()
    {
        var result = Path.Combine(_folder, "semantic.json");
        _exitCode = await Program.Run(["--engine", "semantic", "--model", _folder, "--output", result], TextWriter.Null, TextWriter.Null);
        if (File.Exists(result)) _report = await SemanticSpecificationRunReportFile.ReadFromFile(result);
        _mixedCaseExit = await Program.Run(["--engine", "Semantic", "--model", _folder, "--output", result], TextWriter.Null, TextWriter.Null);
        await using var catalogError = new StringWriter();
        _missingCatalogExit = await Program.Run(["--engine", "semantic", "--model", _folder, "--output", result, "--catalog", Path.Combine(_folder, "missing.catalog")], TextWriter.Null, catalogError);
        _missingCatalogMessage = catalogError.ToString();
        var invalidCatalog = Path.Combine(_folder, "invalid.catalog");
        await File.WriteAllTextAsync(invalidCatalog, "not json");
        await using var invalidCatalogError = new StringWriter();
        _invalidCatalogExit = await Program.Run(["--engine", "semantic", "--model", _folder, "--output", result, "--catalog", invalidCatalog], TextWriter.Null, invalidCatalogError);
        _invalidCatalogMessage = invalidCatalogError.ToString();
        await using var filterError = new StringWriter();
        _structuralFlagExit = await Program.Run(["--engine", "semantic", "--model", _folder, "--output", result, "--slice", "invalid"], TextWriter.Null, filterError);
        _structuralFlagMessage = filterError.ToString();
    }

    [Fact] void should_write_a_versioned_report() => _report.SchemaVersion.ShouldEqual("stage-spec-run/1");
    [Fact] void should_complete_the_semantic_run() => _exitCode.ShouldEqual(0);
    [Fact] void should_preserve_semantic_identity() => _report.Results.Single().SpecificationId.StartsWith("sem1:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_accept_mixed_case_engine() => _mixedCaseExit.ShouldEqual(0);
    [Fact] void should_report_missing_catalog_as_input_error() => Xunit.Assert.True(_missingCatalogExit == 1 && !string.IsNullOrWhiteSpace(_missingCatalogMessage));
    [Fact] void should_report_invalid_catalog_as_input_error() => Xunit.Assert.True(_invalidCatalogExit == 1 && !string.IsNullOrWhiteSpace(_invalidCatalogMessage));
    [Fact] void should_reject_structural_filters() => Xunit.Assert.True(_structuralFlagExit == 2 && _structuralFlagMessage.Contains("structural-only", StringComparison.Ordinal));

    void Destroy() => Directory.Delete(_folder, true);
}
#endif
