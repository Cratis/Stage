// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_a_slice_local_validation_issue : given.a_source_tree
{
    void Establish() => File.WriteAllText(Path.Combine(_root, "nested", "source.play"), """
        module Codes
          feature Registration
            slice StateChange Register
              command Register
                code String identifier
                validate
                  code matches "(?=x)"
                produces Registered
                  for code
              event Registered
        """);

    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_root, [], null, "Codes");

    [Fact] void should_refuse_invalid_source() => _result.Success.ShouldBeFalse();
    [Fact] void should_preserve_the_compiler_error() => Assert.Contains(_result.Errors, error => error.Code == "PLAY0699");
    [Fact] void should_not_downgrade_source_errors_to_warnings() => _result.Notes.ShouldBeEmpty();
}
#endif
