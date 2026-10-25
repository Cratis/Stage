// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specs;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

public class when_generating_command_response_proxies(when_generating_command_response_proxies.context built, ITestOutputHelper output)
    : IClassFixture<when_generating_command_response_proxies.context>
{
    [Fact]
    void should_build_the_rendered_backend_without_warnings()
    {
        output.WriteLine(built.Proxies);
        built.Build.Output.ShouldContain("0 Warning(s)");
    }
    [Fact] void should_type_the_scalar_response_with_the_uuid_value() => Assert.True(built.Proxies.Contains("export class RegisterProject extends Command<IRegisterProject, Guid> implements IRegisterProject {", StringComparison.Ordinal), built.Proxies);
    [Fact] void should_type_the_named_response_with_the_response_record() => Assert.True(built.Proxies.Contains("export class RegisterReceipt extends Command<IRegisterReceipt, RegisterReceiptResponse> implements IRegisterReceipt {", StringComparison.Ordinal), built.Proxies);
    [Fact] void should_not_type_the_single_event_as_a_response() => Assert.True(built.Proxies.Contains("export class PublishProject extends Command<IPublishProject> implements IPublishProject {", StringComparison.Ordinal), built.Proxies);
    [Fact] void should_not_type_the_event_collection_as_a_response() => Assert.True(built.Proxies.Contains("export class PublishProjectTwice extends Command<IPublishProjectTwice> implements IPublishProjectTwice {", StringComparison.Ordinal), built.Proxies);
    [Fact] void should_type_the_scalar_response_alongside_the_event_collection() => Assert.True(built.Proxies.Contains("export class RegisterProjectTwice extends Command<IRegisterProjectTwice, Guid> implements IRegisterProjectTwice {", StringComparison.Ordinal), built.Proxies);
    [Fact] void should_type_the_response_without_an_event() => Assert.True(built.Proxies.Contains("export class AllocateReceipt extends Command<IAllocateReceipt, Guid> implements IAllocateReceipt {", StringComparison.Ordinal), built.Proxies);

    public class context : Specification
    {
        const string AdditionalCommands = """
            slice StateChange PublishProject
              command PublishProject
                projectId ProjectId generated identifier
                name String
                produces event ProjectPublished
                  name String = name
            slice StateChange PublishProjectTwice
              command PublishProjectTwice
                projectId ProjectId generated identifier
                name String
                produces event FirstProjectPublication
                  name String = name
                produces event SecondProjectPublication
                  name String = name
            slice StateChange RegisterProjectTwice
              command RegisterProjectTwice
                projectId ProjectId generated identifier
                name String
                produces event FirstProjectRegistration
                  name String = name
                produces event SecondProjectRegistration
                  name String = name
                returns projectId
        """;

        string _application = null!;

        public ProcessResult Build { get; private set; } = null!;

        public string Proxies { get; private set; } = null!;

        void Establish()
        {
            var plan = invoice_model.Plan(invoice_model.Compile($"{a_generated_values_model.Source}\n{AdditionalCommands}"));
            Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
            _application = SpecTemporaryRoot.NewPath("stage-response-proxies");
            Directory.CreateDirectory(_application);
            foreach (var artifact in plan.Artifacts)
            {
                var path = Path.Combine(_application, artifact.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, [.. artifact.Bytes]);
            }
        }

        async Task Because()
        {
            Build = await ApplicationProcess.Run(_application, TimeSpan.FromMinutes(8), "dotnet", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.True(Build.ExitCode == 0, Build.Output);
            Proxies = string.Join(Environment.NewLine, Directory.EnumerateFiles(Path.Combine(_application, "Projects"), "*.ts", SearchOption.AllDirectories).SelectMany(File.ReadAllLines).Where(line => line.Contains("export class", StringComparison.Ordinal)));
        }

        void Destroy() => SpecTemporaryRoot.Delete(_application);
    }
}
#endif
