// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Pins Screenplay's v7 rejection vector: a specification that supplies a generated property as input never
/// compiles, so Stage reports the compiler's diagnostic and plans nothing.
/// </summary>
public class when_refusing_a_generated_property_supplied_as_input : Specification
{
    readonly CanonicalCorpusRejectionVector _vector = RegisterProjectCorpus.GeneratedPropertySuppliedAsInput;
    string _folder = null!;
    Exception? _error;

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), $"stage-generated-input-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_folder);
        foreach (var document in _vector.SourceForm.Documents)
        {
            File.WriteAllText(Path.Combine(_folder, document.DisplayPath), document.Text);
        }
    }

    void Because() => _error = Record.Exception(() => SemanticModelLoader.LoadFromPathAsync(_folder, null, _vector.ApplicationName).GetAwaiter().GetResult());

    [Fact] void should_expect_the_compiler_diagnostic() => _vector.Diagnostics.Single().Code.ShouldEqual("PLAY0485");
    [Fact] void should_refuse_the_model_before_planning() => _error.ShouldBeOfExactType<InvalidSemanticModel>();
    [Fact] void should_report_the_generated_input() => _error!.Message.Contains(_vector.Diagnostics.Single().Message, StringComparison.Ordinal).ShouldBeTrue();

    void Destroy() => Directory.Delete(_folder, recursive: true);
}
#endif
