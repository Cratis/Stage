// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_writing_one_artifact_fails : a_multi_slice_application
{
    FailingCodeOutput _failingOutput = null!;
    Exception _exception = null!;

    void Establish()
    {
        _failingOutput = new();
        _renderer = new CratisRenderer(
            _scaffolder,
            new Dictionary<SliceType, ISliceRenderer>
            {
                [SliceType.StateChange] = new StateChangeSliceRenderer(),
                [SliceType.StateView] = new StateViewSliceRenderer(),
            },
            _failingOutput);
    }

    async Task Because()
    {
        try
        {
            await _renderer.Render([_application], _targetDirectory, _output, _error);
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_fail_the_render_operation() => _exception.ShouldBeOfExactType<RenderingFailed>();
    [Fact] void should_preserve_the_write_failure() =>
        ((RenderingFailed)_exception).Failures.ShouldContain(failure => failure is SimulatedWriteFailure);
    [Fact] void should_continue_writing_independent_artifacts() =>
        _failingOutput.Files.ShouldContain(file => file.RelativePath.EndsWith(Path.Combine("InvoiceSummary", "InvoiceSummary.cs"), StringComparison.Ordinal));
    [Fact] void should_not_report_unqualified_completion() => _output.ToString().ShouldNotContain("Rendering complete.");
    [Fact] void should_write_the_advisory_failure_marker() => _failingOutput.FailureMarkerWasWritten.ShouldBeTrue();
    [Fact] void should_report_the_failed_write() =>
        _error.ToString().ShouldContain($"Failed to write '{Path.Combine("Billing", "Invoices", "RegisterInvoice", "RegisterInvoice.cs")}'");

    sealed class FailingCodeOutput : ICodeOutput
    {
        readonly List<RenderedFile> _files = [];

        public IReadOnlyList<RenderedFile> Files => _files;
        public bool FailureMarkerWasWritten { get; private set; }

        public Task Write(RenderedFile file, DirectoryInfo targetDirectory, TextWriter output)
        {
            if (file.RelativePath.EndsWith(Path.Combine("RegisterInvoice", "RegisterInvoice.cs"), StringComparison.Ordinal))
            {
                throw new SimulatedWriteFailure();
            }

            _files.Add(file);
            return Task.CompletedTask;
        }

        public Task<bool> TryWriteFailureMarker(DirectoryInfo targetDirectory, TextWriter output)
        {
            FailureMarkerWasWritten = true;
            return Task.FromResult(true);
        }
    }

    sealed class SimulatedWriteFailure : Exception
    {
        public SimulatedWriteFailure()
            : base("Simulated output failure")
        {
        }
    }
}
