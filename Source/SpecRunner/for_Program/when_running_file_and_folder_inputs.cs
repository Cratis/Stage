// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

// Program.Run changes process-wide default culture. This collection must not overlap other collections.
[Collection("SpecRunner application boundary culture")]
public class when_running_file_and_folder_inputs : Specification
{
    const string Source =
        """
        concept InvoiceNumber : String

        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceNumber InvoiceNumber

                validate
                  invoiceNumber not empty message "Invoice number is required"

                produces InvoiceRegistered
                  invoiceNumber = invoiceNumber

              event InvoiceRegistered
                invoiceNumber InvoiceNumber

              specification RegistersAnInvoice
                when RegisterInvoice
                  invoiceNumber = "INV-000001"
                then InvoiceRegistered
                  invoiceNumber = "INV-000001"

              specification OmitsARequiredNumberWithoutExpectingAnError
                when RegisterInvoice
                  invoiceNumber = ""
                then InvoiceRegistered
                  invoiceNumber = ""
        """;

    readonly Dictionary<string, RejectedRun> _rejections = new(StringComparer.Ordinal);
    readonly string _directory = Path.Combine(Path.GetTempPath(), $"stage runner boundary {Guid.NewGuid():N}");
    CultureInfo _culture = CultureInfo.CurrentCulture;
    CultureInfo? _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
    string _folder = null!;
    string _file = null!;
    RunOutput _fromFile = null!;
    RunOutput _fromFolder = null!;
    RunOutput _withInvalidFilters = null!;
    JsonNode _fileJson = null!;
    JsonNode _folderJson = null!;
    JsonNode _invalidFiltersJson = null!;

    void Establish()
    {
        _culture = CultureInfo.CurrentCulture;
        _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        _folder = Path.Combine(_directory, "model input");
        Directory.CreateDirectory(_folder);
        _file = Path.Combine(_folder, "invoices.play");
        File.WriteAllText(_file, Source);
        File.WriteAllText(Path.Combine(_folder, "ignored.play.txt"), "module");
        Directory.CreateDirectory(Path.Combine(_directory, "empty folder"));
        File.WriteAllText(Path.Combine(_directory, "input.txt"), Source);
        File.WriteAllText(Path.Combine(_directory, "input.play.txt"), Source);
        File.WriteAllText(Path.Combine(_directory, "invalid.play"), "module");
    }

    async Task Because()
    {
        var fileOutput = Path.Combine(_directory, "file results", "results.json");
        var folderOutput = Path.Combine(_directory, "folder results", "results.json");
        var filteredOutput = Path.Combine(_directory, "filter results", "results.json");
        _fromFile = await Invoke(["--model", _file, "--output", fileOutput]);
        _fromFolder = await Invoke(["--model", _folder, "--output", folderOutput]);
        _fileJson = JsonNode.Parse(await File.ReadAllTextAsync(fileOutput))!;
        _folderJson = JsonNode.Parse(await File.ReadAllTextAsync(folderOutput))!;

        // Compatibility only: tightening invalid --slice/--spec values into errors is out of scope.
        _withInvalidFilters = await Invoke(["--model", _file, "--output", filteredOutput, "--slice", "not-a-guid", "--spec", "not-a-guid"]);
        _invalidFiltersJson = JsonNode.Parse(await File.ReadAllTextAsync(filteredOutput))!;

        await Reject("no arguments", _ => [], 2, "Required argument 'model'", "Usage:", "--model", "--output");
        await Reject("missing model", output => ["--output", output], 2, "Required argument 'model'", "Usage:", "--model");
        await Reject("missing output", _ => ["--model", _file], 2, "Required argument 'output'", "Usage:", "--output");
        await RejectModel("missing path", "absent.play", "does not exist", ".play");
        await RejectModel("empty folder", "empty folder", "No .play files were found");
        await RejectModel("text file", "input.txt", "must have a .play extension");
        await RejectModel("disguised text file", "input.play.txt", "must have a .play extension");
        await RejectModel("invalid syntax", "invalid.play", "Invalid module declaration");
    }

    [Fact] void should_complete_the_file_run() => _fromFile.ExitCode.ShouldEqual(0);
    [Fact] void should_complete_the_folder_run() => _fromFolder.ExitCode.ShouldEqual(0);
    [Fact] void should_not_report_input_errors_for_completed_runs() => (_fromFile.Error + _fromFolder.Error).ShouldBeEmpty();
    [Fact] void should_announce_nonempty_completed_runs() => new[] { _fromFile, _fromFolder }.All(result => result.Output.Contains("Ran 2 specification(s)", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_the_structured_model_and_all_outcomes_between_file_and_folder() => JsonNode.DeepEquals(_fileJson, _folderJson).ShouldBeTrue();
    [Fact] void should_write_a_nonempty_model_identity() => (_fileJson["eventModelId"]!.GetValue<Guid>() != Guid.Empty).ShouldBeTrue();
    [Fact] void should_discover_both_real_modeled_specifications() => _fileJson["results"]!.AsArray().Count.ShouldEqual(2);
    [Fact] void should_pass_the_structurally_consistent_specification() => ResultNamed("RegistersAnInvoice")["outcome"]!.GetValue<string>().ShouldEqual("Passed");
    [Fact] void should_explicitly_record_the_modeled_failure_despite_a_completed_zero_exit() => ResultNamed("OmitsARequiredNumberWithoutExpectingAnError")["outcome"]!.GetValue<string>().ShouldEqual("Failed");
    [Fact] void should_include_an_explicit_failed_step() => ResultNamed("OmitsARequiredNumberWithoutExpectingAnError")["steps"]!.AsArray().Any(step => step!["outcome"]!.GetValue<string>() == "Failed").ShouldBeTrue();
    [Fact] void should_keep_invalid_optional_identifiers_as_no_filter() => _withInvalidFilters.ExitCode.ShouldEqual(0);
    [Fact] void should_not_drop_modeled_results_for_invalid_optional_identifiers() => JsonNode.DeepEquals(_fileJson, _invalidFiltersJson).ShouldBeTrue();
    [Fact] void should_reject_no_arguments_with_usage_and_without_writing_results() => AssertRejected("no arguments");
    [Fact] void should_reject_a_missing_model_argument_with_usage_and_without_writing_results() => AssertRejected("missing model");
    [Fact] void should_reject_a_missing_output_argument_with_usage_and_without_writing_results() => AssertRejected("missing output");
    [Fact] void should_reject_a_missing_path_without_writing_results() => AssertRejected("missing path");
    [Fact] void should_reject_an_empty_folder_without_writing_results() => AssertRejected("empty folder");
    [Fact] void should_reject_a_text_file_without_writing_results() => AssertRejected("text file");
    [Fact] void should_reject_a_disguised_text_file_without_writing_results() => AssertRejected("disguised text file");
    [Fact] void should_reject_invalid_syntax_without_writing_results() => AssertRejected("invalid syntax");

    Task Destroy()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
            CultureInfo.CurrentCulture = _culture;
        }

        return Task.CompletedTask;
    }

    static async Task<RunOutput> Invoke(string[] args)
    {
        var culture = CultureInfo.CurrentCulture;
        var defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        await using var output = new StringWriter(CultureInfo.InvariantCulture);
        await using var error = new StringWriter(CultureInfo.InvariantCulture);
        try
        {
            var exitCode = await global::Program.Run(args, output, error);
            return new(exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            // Restore even if loading, execution or writing throws; do not replace Specification's lifecycle.
            CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
            CultureInfo.CurrentCulture = culture;
        }
    }

    Task RejectModel(string name, string input, params string[] diagnostics)
    {
        var path = Path.Combine(_directory, input);
        return Reject(name, output => ["--model", path, "--output", output], 1, [path, .. diagnostics]);
    }

    async Task Reject(string name, Func<string, string[]> arguments, int exitCode, params string[] diagnostics)
    {
        var path = Path.Combine(_directory, $"{name} results.json");
        var fresh = await Invoke(arguments(path));
        var createdResults = File.Exists(path);
        byte[] sentinel = [0, 255, 10, 13, 83, 84, 65, 71, 69];
        await File.WriteAllBytesAsync(path, sentinel);
        var existing = await Invoke(arguments(path));
        var remaining = await File.ReadAllBytesAsync(path);
        _rejections.Add(name, new(fresh, existing, exitCode, diagnostics, createdResults, sentinel, remaining));
    }

    JsonNode ResultNamed(string name) => _fileJson["results"]!.AsArray().Single(result => result!["specificationName"]!.GetValue<string>() == name)!;

    void AssertRejected(string name)
    {
        var rejection = _rejections[name];
        foreach (var invocation in new[] { rejection.Fresh, rejection.Existing })
        {
            invocation.ExitCode.ShouldEqual(rejection.ExitCode);
            invocation.Output.ShouldBeEmpty();
            foreach (var diagnostic in rejection.Diagnostics)
            {
                invocation.Error.Contains(diagnostic, StringComparison.Ordinal).ShouldBeTrue();
            }
        }

        rejection.CreatedResults.ShouldBeFalse();
        rejection.Remaining.SequenceEqual(rejection.Sentinel).ShouldBeTrue();
    }

    sealed record RunOutput(int ExitCode, string Output, string Error);

    sealed record RejectedRun(RunOutput Fresh, RunOutput Existing, int ExitCode, string[] Diagnostics, bool CreatedResults, byte[] Sentinel, byte[] Remaining);

    // xUnit's existing collection mechanism isolates the process-global mutation without an assembly-wide switch.
    [CollectionDefinition("SpecRunner application boundary culture", DisableParallelization = true)]
    public class culture_collection;
}
#endif
