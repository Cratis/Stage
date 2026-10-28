// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_values_and_responses(when_executing_generated_values_and_responses.context fixture) : IClassFixture<when_executing_generated_values_and_responses.context>
{
    [Fact] void should_compile_without_warnings() => fixture.BuildWarningLines.ShouldEqual(string.Empty);
    [Fact] void should_execute_pinned_scalar_record_and_unpinned_relational_specifications() => fixture.Test.ShouldContain("Passed!");

    public class context : a_generated_application
    {
        protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile(a_generated_values_model.Source));

        async Task Because()
        {
            Build = await Run("generated-values-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "-p:NoWarn=CS7022", "--nologo");
            Test = await Run("generated-values-test.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
        }

        public string Build { get; private set; } = null!;
        public string Test { get; private set; } = null!;
        public string BuildWarningLines => BuildWarnings(Build);
    }
}
#endif
