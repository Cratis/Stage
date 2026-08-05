// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SliceNaming;

public class when_computing_names : Specification
{
    string _namespace = null!;
    string _typeName = null!;
    string _fileName = null!;
    IReadOnlyList<string> _folderPath = null!;

    void Because()
    {
        _namespace = SliceNaming.Namespace("CratisApp", ["billing", "invoices"]);
        _typeName = SliceNaming.TypeName("register invoice");
        _fileName = SliceNaming.FileName("register invoice");
        _folderPath = SliceNaming.FolderPath(["billing", "invoices"]);
    }

    [Fact] void should_build_the_namespace_from_root_and_path() => _namespace.ShouldEqual("CratisApp.Billing.Invoices");
    [Fact] void should_pascal_case_the_type_name() => _typeName.ShouldEqual("RegisterInvoice");
    [Fact] void should_suffix_the_file_name_with_cs() => _fileName.ShouldEqual("RegisterInvoice.cs");
    [Fact] void should_pascal_case_every_folder_segment() => _folderPath.ShouldContainOnly("Billing", "Invoices");
}
