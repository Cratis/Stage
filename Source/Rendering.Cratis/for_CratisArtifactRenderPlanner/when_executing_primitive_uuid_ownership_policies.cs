// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_uuid_ownership_policies.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_primitive_uuid_ownership_policies(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_and_release_without_warnings() => (fixture.DebugWarnings + fixture.ReleaseWarnings).ShouldBeEmpty();
    [Fact] void should_preserve_allow_and_deny_for_primitive_uuid_targets() => fixture.Results.Select(result => result.Outcome).ShouldContainOnly(["Passed", "Passed", "Passed", "Passed", "Passed"]);

    public class context : when_executing_uuid_ownership_policies.context
    {
        protected override string ModelSource
        {
            get
            {
                var source = base.ModelSource.Replace("invoiceId InvoiceId", "invoiceId Uuid", StringComparison.Ordinal);
                return source[..source.IndexOf("    slice StateView", StringComparison.Ordinal)];
            }
        }

        protected override string IdentifierExpression => "new Guid(key)";
        protected override bool IncludeQuery => false;
    }
}
#endif
