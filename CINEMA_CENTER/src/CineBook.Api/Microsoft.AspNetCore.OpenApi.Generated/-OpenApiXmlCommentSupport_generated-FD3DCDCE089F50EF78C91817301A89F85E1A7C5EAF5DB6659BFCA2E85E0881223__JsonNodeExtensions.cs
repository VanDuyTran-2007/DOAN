using System.CodeDom.Compiler;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.OpenApi.Generated;

[GeneratedCode("Microsoft.AspNetCore.OpenApi.SourceGenerators, Version=10.0.12.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "10.0.12.0")]
internal static class _003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__JsonNodeExtensions
{
	public static JsonNode? Parse(this string? json)
	{
		if (json == null)
		{
			return null;
		}
		try
		{
			return JsonNode.Parse(json);
		}
		catch (JsonException)
		{
			try
			{
				return JsonNode.Parse("\"" + json.Replace("\"", "\\\"") + "\"");
			}
			catch (JsonException)
			{
				return null;
			}
		}
	}
}
