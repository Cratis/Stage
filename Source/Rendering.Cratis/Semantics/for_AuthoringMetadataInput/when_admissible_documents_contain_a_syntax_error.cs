// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_AuthoringMetadataInput;

public class when_admissible_documents_contain_a_syntax_error : given.an_admissible_compilation
{
    Exception? _error;

    void Establish()
    {
        // Retain source coordinates so the compilation's source map remains valid.
        var documents = ReplaceSource(_compilation, "concept ProjectName : String", "concept ProjectName ! String");
        _compilation = SemanticCompilation.Create(_compilation.Model, documents, _compilation.SourceMap);
    }

    void Because() => _error = Catch.Exception(() => AuthoringMetadataInput.Create(_compilation));

    [Fact] void should_refuse_the_invalid_source() => _error.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_keep_the_integrity_error() => _error.Message.ShouldEqual("Authoring metadata requires the syntax and identity catalog from the same semantic compilation.");
}
