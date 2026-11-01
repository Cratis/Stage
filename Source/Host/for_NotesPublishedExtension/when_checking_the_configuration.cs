// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Showrunner.Notes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_NotesPublishedExtension;

public class when_checking_the_configuration
{
    static NotesOptions Bind(Dictionary<string, string?> values)
    {
        return NotesPublishedExtension.Bind(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }

    static Dictionary<string, string?> Complete() => new()
    {
        ["Notes:ApplicationName"] = "Ledger",
        ["Notes:PublicationId"] = "pub-1",
        ["Notes:Transport"] = "Http",
        ["Notes:CollectorUrl"] = "https://collector.example.invalid",
        ["Notes:CollectorToken"] = "x",
        ["Notes:CollectorTenantId"] = "tenant"
    };

    [Fact] void should_be_registered_as_the_notes_extension() => PublishedHostExtensions.Registered.ShouldContain(extension => extension.Name == PublishedHostExtensions.Notes);
    [Fact]
    void should_read_the_strongly_typed_options_from_the_environment_forms()
    {
        var options = Bind(Complete());
        options.ApplicationName.Value.ShouldEqual("Ledger");
        options.PublicationId.Value.ShouldEqual("pub-1");
        options.CollectorTenantId.Value.ShouldEqual("tenant");
        options.Transport.ShouldEqual(NotesTransport.Http);
    }

    [Fact] void should_accept_a_complete_configuration() => NotesPublishedExtension.Problems(Bind(Complete())).ShouldBeEmpty();

    [Fact]
    void should_refuse_a_missing_application_name()
    {
        var values = Complete();
        values.Remove("Notes:ApplicationName");
        NotesPublishedExtension.Problems(Bind(values)).ShouldNotBeEmpty();
    }

    [Fact]
    void should_refuse_a_missing_publication()
    {
        var values = Complete();
        values.Remove("Notes:PublicationId");
        NotesPublishedExtension.Problems(Bind(values)).ShouldNotBeEmpty();
    }

    [Fact]
    void should_refuse_the_http_transport_without_a_collector()
    {
        var values = Complete();
        values.Remove("Notes:CollectorUrl");
        NotesPublishedExtension.Problems(Bind(values)).ShouldNotBeEmpty();
    }

    [Fact]
    void should_refuse_the_http_transport_without_a_secret()
    {
        var values = Complete();
        values.Remove("Notes:CollectorToken");
        NotesPublishedExtension.Problems(Bind(values)).ShouldNotBeEmpty();
    }

    [Fact]
    void should_refuse_start_when_enabled_and_misconfigured()
    {
        var builder = WebApplication.CreateBuilder();
        var context = new PublishedHostContext(builder, null!, null!, "store");
        Assert.Throws<PublishedModeRefused>(() => new NotesPublishedExtension().ConfigureServices(context));
    }
}
