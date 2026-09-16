// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_validating_file_and_folder_inputs : given.a_compiled_invoicing_model
{
    // These declarations narrow the invoicing fixture to a shared concept and two owning slices.
    const string Concepts = "concept Money : Decimal";
    const string ModuleShell = "module Invoicing\n  feature InvoiceManagement\n";
    const string EventOwner =
        """
        slice StateChange RecordInvoice
          event InvoiceRegistered
            amount Money
        """;
    const string CommandOwner =
        """
        slice StateChange RegisterInvoice
          command RegisterInvoice
            amount Money
            produces InvoiceRegistered
              amount = amount
        """;

    string _directory = null!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage loader boundaries {Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [Theory]
    [InlineData("missing.play", "The path does not exist.")]
    [InlineData("empty", "No .play files were found.")]
    [InlineData("input.txt", "The selected file must have a .play extension.")]
    [InlineData("input.play.txt", "The selected file must have a .play extension.")]
    public async Task should_distinguish_input_failures_from_compiler_errors(string input, string diagnostic)
    {
        Directory.CreateDirectory(Path.Combine(_directory, "empty"));
        WriteSource("input.txt", Source);
        WriteSource("input.play.txt", Source);
        var path = Path.Combine(_directory, input);

        var failure = await Catch.Exception(() => EventModelLoader.LoadFromPathAsync(path));

        failure.ShouldBeOfExactType<InvalidEventModel>();
        failure.Message.Contains($"'{path}'", StringComparison.Ordinal).ShouldBeTrue();
        failure.Message.Contains(diagnostic, StringComparison.Ordinal).ShouldBeTrue();
        failure.Message.Contains("(1,1):", StringComparison.Ordinal).ShouldBeFalse();
    }
    [Theory]
    [InlineData("input.play")]
    [InlineData("input.PLAY")]
    public async Task should_preserve_meaningful_serialized_slices_for_either_extension(string fileName)
    {
        var file = WriteSource(fileName, Source);
        WriteSource("ignored.play.txt", "module");

        var fromFile = await EventModelLoader.LoadFromPathAsync(file);
        var fromFolder = await EventModelLoader.LoadFromPathAsync(_directory);

        var slices = fromFile.Collections.Single().Modules.Single().Features.Single().Slices;
        slices.Count.ShouldEqual(4);
        var registration = slices.Single(slice => slice.Name == "RegisterInvoice");
        registration.Command!.Name.ShouldEqual("RegisterInvoice");
        registration.Events.Single().Name.ShouldEqual("InvoiceRegistered");
        registration.Specifications.Single().ShouldNotBeNull();
        slices.Single(slice => slice.Name == "InvoiceList").ReadModel.ShouldNotBeNull();
        fromFile.Id.ShouldEqual(_model.Id);
        SerializedSlices(fromFile).GetRawText().ShouldEqual(SerializedSlices(fromFolder).GetRawText());
        SerializedSlices(fromFile).GetRawText().ShouldEqual(SerializedSlices(_model).GetRawText());
        EventModelFile.Write(fromFile).ShouldEqual(EventModelFile.Write(fromFolder));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_report_deterministic_relative_locations_for_real_syntax_errors(bool selectFile)
    {
        // The existing invalid-input spec establishes "module" as a real parser error, not an unknown-reference warning.
        var file = WriteSource("nested/a.play", "module");
        WriteSource("nested/z.play", "module");
        var path = selectFile ? file : _directory;

        var first = await Catch.Exception(() => EventModelLoader.LoadFromPathAsync(path));
        var second = await Catch.Exception(() => EventModelLoader.LoadFromPathAsync(path));

        first.ShouldBeOfExactType<InvalidEventModel>();
        second.ShouldBeOfExactType<InvalidEventModel>();
        first.Message.ShouldEqual(second.Message);
        var diagnostics = first.Message.Split(Environment.NewLine).Skip(1).ToArray();
        diagnostics.Length.ShouldEqual(selectFile ? 1 : 2);
        diagnostics[0].StartsWith($"{(selectFile ? "a.play" : "nested/a.play")}(1,1): Invalid module declaration", StringComparison.Ordinal).ShouldBeTrue();
        if (!selectFile)
        {
            diagnostics[1].StartsWith("nested/z.play(1,1): Invalid module declaration", StringComparison.Ordinal).ShouldBeTrue();
        }
        diagnostics.Any(diagnostic => diagnostic.Contains(_directory, StringComparison.Ordinal)).ShouldBeFalse();
    }
    [Theory]
    [InlineData("Decimal")]
    [InlineData("String")]
    public async Task should_reject_duplicate_application_concepts_across_files(string secondType)
    {
        var firstSource = Concepts + "\n" + ModuleShell + IndentOwner(EventOwner);
        var secondSource = $"concept Money : {secondType}\n" + ModuleShell + IndentOwner(CommandOwner);
        WriteSource("a.play", firstSource);
        WriteSource("nested/b.play", secondSource);

        // Both complete files load alone; an unresolved event reference may warn, but is not an error.
        EventModelLoader.LoadFromSource(firstSource).Collections.Single().Modules.Single().Features.Single().Slices.Count.ShouldEqual(1);
        EventModelLoader.LoadFromSource(secondSource).Collections.Single().Modules.Single().Features.Single().Slices.Count.ShouldEqual(1);

        var compilation = new PlayFileCompiler().CompileFolder(_directory);
        compilation.Result.Success.ShouldBeFalse();
        var duplicate = compilation.Result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RepeatedDeclarationAcrossFiles);
        duplicate.Severity.ShouldEqual(DiagnosticSeverity.Error);
        duplicate.Location.Path.ShouldEqual("nested/b.play");
        duplicate.Message.Contains("Money", StringComparison.Ordinal).ShouldBeTrue();

        var failure = await Catch.Exception(() => EventModelLoader.LoadFromPathAsync(_directory));

        failure.ShouldBeOfExactType<InvalidEventModel>();
        failure.Message.Contains($"'{_directory}'", StringComparison.Ordinal).ShouldBeTrue();
        failure.Message.Contains("Money", StringComparison.Ordinal).ShouldBeTrue();
        failure.Message.Contains("nested/b.play(", StringComparison.Ordinal).ShouldBeTrue();
        failure.Message.Contains("No .play files", StringComparison.Ordinal).ShouldBeFalse();
    }
    [Theory]
    [InlineData("event", "RecordInvoice", "RecordAnotherInvoice")]
    [InlineData("command", "RegisterInvoice", "RequestInvoice")]
    public async Task should_follow_compiler_admission_for_same_names_in_distinct_owning_slices(string declaration, string firstSlice, string secondSlice)
    {
        WriteSource("concepts.play", Concepts);
        var owner = declaration == "event" ? EventOwner : CommandOwner;
        WriteSource("a.play", ModuleShell + IndentOwner(owner));
        var otherOwner = declaration == "event"
            ? owner.Replace("slice StateChange RecordInvoice", "slice StateChange RecordAnotherInvoice", StringComparison.Ordinal)
            : owner.Replace("slice StateChange RegisterInvoice", "slice StateChange RequestInvoice", StringComparison.Ordinal);
        WriteSource("nested/b.play", ModuleShell + IndentOwner(otherOwner));

        // These scoped names are admitted by the pinned syntax compiler, not a runtime identity policy.
        EventModelLoader.LoadFromSource(Concepts + "\n" + ModuleShell + IndentOwner(owner)).Collections.Single().Modules.Single().Features.Single().Slices.Count.ShouldEqual(1);
        EventModelLoader.LoadFromSource(Concepts + "\n" + ModuleShell + IndentOwner(otherOwner)).Collections.Single().Modules.Single().Features.Single().Slices.Count.ShouldEqual(1);

        var compilation = new PlayFileCompiler().CompileFolder(_directory);
        compilation.Result.Success.ShouldBeTrue();

        var model = await EventModelLoader.LoadFromPathAsync(_directory);

        var slices = model.Collections.Single().Modules.Single().Features.Single().Slices;
        slices.Count.ShouldEqual(2);
        slices.Any(slice => slice.Name == firstSlice).ShouldBeTrue();
        slices.Any(slice => slice.Name == secondSlice).ShouldBeTrue();
    }
    [Theory]
    [InlineData("model", "file")]
    [InlineData("scene", "file")]
    [InlineData("render", "file")]
    [InlineData("model", "missing")]
    [InlineData("scene", "missing")]
    [InlineData("render", "missing")]
    [InlineData("model", "empty")]
    [InlineData("scene", "empty")]
    [InlineData("render", "empty")]
    [InlineData("model", "invalid")]
    [InlineData("scene", "invalid")]
    [InlineData("render", "invalid")]
    public async Task should_keep_directory_only_entry_points_at_the_same_input_boundary(string entryPoint, string input)
    {
        var file = WriteSource("selected.play", Source);
        var empty = Path.Combine(_directory, "empty");
        Directory.CreateDirectory(empty);
        WriteSource("invalid/nested/bad.play", "module");
        var path = input == "file" ? file : Path.Combine(_directory, input);
        var diagnostic = input switch
        {
            "empty" => "No .play files were found.",
            "invalid" => "nested/bad.play(1,1): Invalid module declaration",
            _ => "The directory does not exist."
        };

        var failure = await Catch.Exception(() => LoadDirectory(entryPoint, path));

        failure.ShouldBeOfExactType<InvalidEventModel>();
        failure.Message.Contains($"'{path}'", StringComparison.Ordinal).ShouldBeTrue();
        failure.Message.Contains(diagnostic, StringComparison.Ordinal).ShouldBeTrue();
    }
    [Fact]
    public async Task should_merge_repeated_module_shells_before_resolving_split_declarations()
    {
        WriteSource("split/00-concepts.play", Concepts);
        WriteSource("split/10-events.play", ModuleShell + IndentOwner(EventOwner));
        WriteSource("split/nested/20-commands.PLAY", ModuleShell + IndentOwner(CommandOwner));
        var combined = WriteSource("combined.play", Concepts + "\n" + ModuleShell + IndentOwner(EventOwner) + "\n" + IndentOwner(CommandOwner));

        var fromFolder = await EventModelLoader.LoadFromPathAsync(Path.Combine(_directory, "split"));
        var fromFile = await EventModelLoader.LoadFromPathAsync(combined);

        var collection = fromFolder.Collections.Single();
        var module = collection.Modules.Single();
        var feature = module.Features.Single();
        feature.Slices.Count.ShouldEqual(2);
        module.Name.ShouldEqual("Invoicing");
        feature.Name.ShouldEqual("InvoiceManagement");
        collection.EventModelId.ShouldEqual(fromFolder.Id);
        module.EventModelId.ShouldEqual(fromFolder.Id);
        var command = feature.Slices.Single(slice => slice.Name == "RegisterInvoice").Command!;
        var ownedEvent = feature.Slices.Single(slice => slice.Name == "RecordInvoice").Events.Single();
        command.Produces.Single().Event.ShouldEqual(ownedEvent.Name);
        ownedEvent.SourceEventId.ShouldEqual(string.Empty);
        using var commandSchema = JsonDocument.Parse(command.Schema);
        using var eventSchema = JsonDocument.Parse(ownedEvent.Schema);
        commandSchema.RootElement.GetProperty("properties").GetProperty("amount").GetProperty("type").GetString().ShouldEqual("number");
        eventSchema.RootElement.GetProperty("properties").GetProperty("amount").GetProperty("type").GetString().ShouldEqual("number");
        SerializedSlices(fromFolder).GetRawText().ShouldEqual(SerializedSlices(fromFile).GetRawText());
        EventModelFile.Write(fromFolder).ShouldEqual(EventModelFile.Write(fromFile));
    }
    [Fact]
    public async Task should_not_promote_unknown_reference_warnings_to_loader_errors()
    {
        var compilation = new ScreenplayCompiler().Compile(Source);
        compilation.Success.ShouldBeTrue();
        compilation.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeTrue();
        compilation.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeFalse();
        WriteSource("warnings.play", Source);

        var model = await EventModelLoader.LoadFromPathAsync(_directory);

        EventModelFile.Write(model).ShouldEqual(EventModelFile.Write(_model));
        model.Collections.Single().Modules.Single().Features.Single().Slices.Count.ShouldEqual(4);
    }
    [Fact]
    public async Task should_load_the_scene_through_the_real_directory_compiler()
    {
        WriteSource("nested/invoicing.play", Source);

        var application = await EventModelLoader.LoadSceneApplicationFromDirectoryAsync(_directory);

        application.Layouts.Single().Name.ShouldEqual(DefaultLayout.Name);
        application.Layouts.Single().Slots.Single().Name.ShouldEqual(DefaultLayout.ContentSlotName);
    }
    [Fact]
    public async Task should_return_render_findings_rather_than_input_errors_for_valid_source_without_targets()
    {
        WriteSource("nested/invoicing.play", Source);

        var plan = await EventModelLoader.LoadRenderPlanFromDirectoryAsync(_directory, []);

        plan.Findings.Single().Kind.ShouldEqual(RenderFindingKind.NoTargetDeclared);
        plan.IsComplete.ShouldBeFalse();
    }

    static string IndentOwner(string source) => string.Join('\n', source.Split('\n').Select(line => "    " + line));

    static JsonElement SerializedSlices(EventModel model)
    {
        using var document = JsonDocument.Parse(EventModelFile.Write(model));
        return document.RootElement.GetProperty("collections")[0].GetProperty("modules")[0].GetProperty("features")[0].GetProperty("slices").Clone();
    }

    static Task LoadDirectory(string entryPoint, string path) => entryPoint switch
    {
        "model" => EventModelLoader.LoadFromDirectoryAsync(path),
        "scene" => EventModelLoader.LoadSceneApplicationFromDirectoryAsync(path),
        _ => EventModelLoader.LoadRenderPlanFromDirectoryAsync(path, [])
    };

    string WriteSource(string relativePath, string source)
    {
        var path = Path.Combine(_directory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, source);
        return path;
    }

    void Destroy()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
#endif
