using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Microsoft.AspNetCore.OpenApi.Generated;

[GeneratedCode("Microsoft.AspNetCore.OpenApi.SourceGenerators, Version=10.0.12.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "10.0.12.0")]
internal static class _003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__DocumentationCommentIdHelper
{
	public static string CreateDocumentationId(this Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		return "T:" + GetTypeDocId(type, includeGenericArguments: false, omitGenericArity: false);
	}

	public static string CreateDocumentationId(this PropertyInfo property)
	{
		if (property == null)
		{
			throw new ArgumentNullException("property");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("P:");
		if (property.DeclaringType != null)
		{
			stringBuilder.Append(GetTypeDocId(property.DeclaringType, includeGenericArguments: false, omitGenericArity: false));
		}
		stringBuilder.Append('.');
		stringBuilder.Append(property.Name);
		ParameterInfo[] indexParameters = property.GetIndexParameters();
		if (indexParameters.Length != 0)
		{
			stringBuilder.Append('(');
			for (int i = 0; i < indexParameters.Length; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(GetTypeDocId(indexParameters[i].ParameterType, includeGenericArguments: true, omitGenericArity: false));
			}
			stringBuilder.Append(')');
		}
		return stringBuilder.ToString();
	}

	public static string CreateDocumentationId(Type containerType, string propertyName)
	{
		if (containerType == null)
		{
			throw new ArgumentNullException("containerType");
		}
		if (string.IsNullOrEmpty(propertyName))
		{
			throw new ArgumentException("Property name cannot be null or empty.", "propertyName");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("P:");
		stringBuilder.Append(GetTypeDocId(containerType, includeGenericArguments: false, omitGenericArity: false));
		stringBuilder.Append('.');
		stringBuilder.Append(propertyName);
		return stringBuilder.ToString();
	}

	public static string CreateDocumentationId(this MethodInfo method)
	{
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("M:");
		if (method.DeclaringType != null)
		{
			stringBuilder.Append(GetTypeDocId(method.DeclaringType, includeGenericArguments: false, omitGenericArity: false));
		}
		stringBuilder.Append('.');
		if (method.IsConstructor)
		{
			stringBuilder.Append(method.IsStatic ? "#cctor" : "#ctor");
		}
		else
		{
			stringBuilder.Append(method.Name);
			if (method.IsGenericMethod)
			{
				stringBuilder.Append("``");
				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}", method.GetGenericArguments().Length);
			}
		}
		ParameterInfo[] parameters = method.GetParameters();
		if (parameters.Length != 0)
		{
			stringBuilder.Append('(');
			for (int i = 0; i < parameters.Length; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(GetTypeDocId(parameters[i].ParameterType, includeGenericArguments: true, omitGenericArity: true));
			}
			stringBuilder.Append(')');
		}
		if (method.ReturnType != typeof(void))
		{
			stringBuilder.Append('~');
			stringBuilder.Append(GetTypeDocId(method.ReturnType, includeGenericArguments: true, omitGenericArity: true));
		}
		return stringBuilder.ToString();
	}

	private static string GetTypeDocId(Type type, bool includeGenericArguments, bool omitGenericArity)
	{
		if (type.IsGenericParameter)
		{
			if (type.DeclaringMethod != null)
			{
				return "``" + type.GenericParameterPosition;
			}
			if (type.DeclaringType != null)
			{
				return "`" + type.GenericParameterPosition;
			}
			return type.Name;
		}
		if (type.IsGenericType)
		{
			Type genericTypeDefinition = type.GetGenericTypeDefinition();
			string text = genericTypeDefinition.FullName ?? genericTypeDefinition.Name;
			StringBuilder stringBuilder = new StringBuilder(text.Length);
			foreach (char c in text)
			{
				switch (c)
				{
				case '+':
					stringBuilder.Append('.');
					continue;
				default:
					stringBuilder.Append(c);
					continue;
				case '`':
					break;
				}
				break;
			}
			if (!omitGenericArity)
			{
				int num = genericTypeDefinition.GetGenericArguments().Length;
				stringBuilder.Append('`');
				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}", num);
			}
			if (includeGenericArguments && !type.IsGenericTypeDefinition)
			{
				Type[] genericArguments = type.GetGenericArguments();
				stringBuilder.Append('{');
				for (int j = 0; j < genericArguments.Length; j++)
				{
					if (j > 0)
					{
						stringBuilder.Append(',');
					}
					stringBuilder.Append(GetTypeDocId(genericArguments[j], includeGenericArguments, omitGenericArity));
				}
				stringBuilder.Append('}');
			}
			return stringBuilder.ToString();
		}
		return (type.FullName ?? type.Name).Replace('+', '.');
	}

	public static string NormalizeDocId(string docId)
	{
		int num = docId.IndexOf('~');
		if (num == -1)
		{
			return docId;
		}
		if (docId.Contains("op_Implicit") || docId.Contains("op_Explicit"))
		{
			return docId;
		}
		return docId.Substring(0, num);
	}
}
