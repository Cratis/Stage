// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Rendering.Templates;

internal static class TemplateSurface
{
    internal static string Digest => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Describe())));

    static string Describe()
    {
        var surface = new StringBuilder("stage-templates/v1\nscriban:7.5.3\n");
        foreach (var entry in LockedBuiltins.Members.OrderBy(entry => entry.Key, StringComparer.Ordinal)) surface.Append(entry.Key).Append(':').AppendJoin(',', entry.Value.Order(StringComparer.Ordinal)).Append('\n');
        surface.Append("name:words,pascal,camel,snake,kebab,constant\nlit:string,integer,decimal,boolean,null,comment,identifier\nsym:decl,ref,imports\ntype:ref\nexpr:supported,lower\nrender(name,arg?)\nsection(name,default_partial)\nrefuse(code,message)\ngap(code,role,intent,modelPath)\n")
            .Append("steps:2000000;bytes:8388608;artifacts:2000;fanOut:2000;string:262144;recursion:64;deadline:10s\n")
            .Append("text:65536;nesting:32;jsonDepth:64;integers:Int64;fractions:Decimal\n")
            .Append("contracts:templates/v1.0,naming/v1.0,operators/v1.0,literals/v1.0,symbols/v1.0\n");
        return surface.ToString();
    }
}
