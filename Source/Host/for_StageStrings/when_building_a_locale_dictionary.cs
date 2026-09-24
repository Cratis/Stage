// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageStrings;

public class when_building_a_locale_dictionary : Specification
{
    string _root = null!;
    StageStrings _strings = null!;
    IReadOnlyDictionary<string, string> _dictionary = null!;

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("stage-strings-spec").FullName;
        Directory.CreateDirectory(Path.Combine(_root, "Invoicing"));

        // Two files declare the same locale - a module-level one and a slice-level one, the same way a real
        // model pairs a .strings file with every .play file that names a $strings.<key>.
        File.WriteAllText(
            Path.Combine(_root, "Invoicing.en.strings"),
            "invoice.number = \"Invoice #\"\ninvoice.customer = \"Customer\"\n");
        File.WriteAllText(
            Path.Combine(_root, "Invoicing", "InvoiceList.en.strings"),
            "invoice.listTitle = \"Invoices\"\n");
        File.WriteAllText(
            Path.Combine(_root, "Invoicing.no.strings"),
            "invoice.number = \"Fakturanr\"\n");

        _strings = new(_root);
    }

    void Because() => _dictionary = _strings.Dictionary("en");

    void Destroy() => Directory.Delete(_root, recursive: true);

    [Fact] void should_include_a_key_from_the_module_level_file() => _dictionary["invoice.number"].ShouldEqual("Invoice #");
    [Fact] void should_include_a_key_from_the_nested_slice_level_file() => _dictionary["invoice.listTitle"].ShouldEqual("Invoices");
    [Fact] void should_take_the_requested_locales_value_rather_than_another_locales_file() => _dictionary["invoice.number"].ShouldEqual("Invoice #");
    [Fact] void should_report_every_locale_at_least_one_file_declares() => _strings.Locales().ShouldContainOnly("en", "no");
}
