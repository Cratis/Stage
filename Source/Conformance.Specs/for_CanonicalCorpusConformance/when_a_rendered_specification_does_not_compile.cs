// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_a_rendered_specification_does_not_compile : a_rendered_canonical_vector
{
    IReadOnlyList<compile_error> _errors = null!;
    string _path = null!;

    void Because()
    {
        _path = _plan.Artifacts.First(file => Path.GetFileName(file.RelativePath).StartsWith("when_", StringComparison.Ordinal) && file.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).RelativePath;
        _errors = rendered_compilation.Errors(Corrupt(_path));
    }

    [Fact] void should_report_cs0246() => _errors.Select(error => error.Code).ShouldContain("CS0246");
    [Fact] void should_report_the_specification_path() => _errors.Select(error => error.Path).ShouldContain(_path);
}
#endif
