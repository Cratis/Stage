// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Languages.Java.for_InlineBodyScanner.given;

public class a_native_body : Specification
{
    protected string _body = string.Empty;
    protected IReadOnlySet<string> _identifiers = null!;

    protected void Scan() => _identifiers = InlineBodyScanner.Identifiers(_body);
}
