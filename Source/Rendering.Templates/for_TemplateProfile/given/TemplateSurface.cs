// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Scriban.Runtime;

namespace Cratis.Stage.Rendering.Templates;

internal static class TemplateSurface
{
    internal static string Digest => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Describe())));

    static string TypeName(Type type) => type.IsGenericType ? type.GetGenericTypeDefinition().FullName + "[" + string.Join(',', type.GenericTypeArguments.Select(TypeName)) + "]" : type.FullName ?? type.Name;

    static string Describe()
    {
        var lines = new List<string> { TemplateProfile.Id, "scriban:7.5.3", TemplateCatalogue.Signatures, HelperCatalogue.Canonical };
        foreach (var type in typeof(TemplateProfile).Assembly.ExportedTypes.Where(type => type.Namespace?.Contains(".for_", StringComparison.Ordinal) != true).OrderBy(TypeName, StringComparer.Ordinal))
        {
            lines.Add("type:" + TypeName(type) + ":" + (type.BaseType is null ? string.Empty : TypeName(type.BaseType)) + ":" + string.Join(',', type.GetInterfaces().Select(TypeName).Order(StringComparer.Ordinal)));
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                lines.Add(TypeName(type) + ":" + member.MemberType + ":" + member);
                if (member is FieldInfo { IsLiteral: true } field) lines.Add(TypeName(type) + ":" + field.Name + "=" + Convert.ToString(field.GetRawConstantValue(), CultureInfo.InvariantCulture));
                if (member is MethodBase method)
                {
                    foreach (var parameter in method.GetParameters()) lines.Add(TypeName(type) + ":" + method.Name + ":" + parameter.Position.ToString(CultureInfo.InvariantCulture) + ":" + parameter.Name + ":" + parameter.HasDefaultValue + ":" + Convert.ToString(parameter.DefaultValue, CultureInfo.InvariantCulture));
                }
            }
        }

        var builtins = LockedBuiltins.Create();
        foreach (var (name, members) in LockedBuiltins.Members)
        {
            foreach (var member in members)
            {
                var function = (IScriptFunctionInfo)((ScriptObject)builtins[name]!)[member]!;
                var parameters = Enumerable.Range(0, function.ParameterCount).Select(function.GetParameterInfo).Select(parameter => TypeName(parameter.ParameterType) + " " + parameter.Name + (parameter.HasDefaultValue ? "=" + Convert.ToString(parameter.DefaultValue, CultureInfo.InvariantCulture) : string.Empty));
                lines.Add("builtin:" + name + "." + member + "(" + string.Join(',', parameters) + ")->" + TypeName(function.ReturnType) + ":required=" + function.RequiredParameterCount.ToString(CultureInfo.InvariantCulture) + ":var=" + function.VarParamKind);
            }
        }

        foreach (var field in typeof(TemplateCodes).GetFields(BindingFlags.Public | BindingFlags.Static)) lines.Add("code:" + field.Name + "=" + field.GetRawConstantValue());
        lines.Add("limits:" + TemplateBudget.Default);
        lines.Add("prescan:" + TemplateProfile.MaxTemplateBytes + ":" + TemplateProfile.MaxDepth);
        lines.Add("roles:" + string.Join(',', TemplateRoles.Names));
        lines.Add("refuse:" + string.Join(',', TemplatePartialLint.Categories));
        lines.Add("helpers:" + string.Join(';', TemplateCatalogue.Helpers.Select(entry => entry.Key + ":" + string.Join(',', entry.Value))));
        return string.Join('\n', lines.Order(StringComparer.Ordinal)) + "\n";
    }
}
