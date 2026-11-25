using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi;

namespace Microsoft.AspNetCore.OpenApi.Generated;

[GeneratedCode("Microsoft.AspNetCore.OpenApi.SourceGenerators, Version=10.0.12.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "10.0.12.0")]
internal class _003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlCommentSchemaTransformer : IOpenApiSchemaTransformer
{
	public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
	{
		if (_003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlCommentCache.Cache.TryGetValue(_003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__DocumentationCommentIdHelper.NormalizeDocId(context.JsonTypeInfo.Type.CreateDocumentationId()), out _003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlComment value))
		{
			schema.Description = value.Summary;
			string text = value.Examples?.FirstOrDefault();
			if (text != null)
			{
				int num = 1;
				List<JsonNode> list = new List<JsonNode>(num);
				CollectionsMarshal.SetCount(list, num);
				CollectionsMarshal.AsSpan(list)[0] = text.Parse();
				schema.Examples = list;
			}
		}
		JsonPropertyInfo jsonPropertyInfo = context.JsonPropertyInfo;
		if (jsonPropertyInfo != null && jsonPropertyInfo.AttributeProvider is PropertyInfo property && _003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlCommentCache.Cache.TryGetValue(_003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__DocumentationCommentIdHelper.NormalizeDocId(property.CreateDocumentationId()), out _003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlComment value2))
		{
			string text2 = value2.Summary;
			if (!string.IsNullOrEmpty(text2) && !string.IsNullOrEmpty(value2.Value))
			{
				text2 = text2 + "\n" + value2.Value;
			}
			else if (string.IsNullOrEmpty(text2))
			{
				text2 = value2.Value;
			}
			if (schema.Metadata == null || !schema.Metadata.TryGetValue("x-schema-id", out object value3) || string.IsNullOrEmpty(value3 as string))
			{
				schema.Description = text2;
				string text3 = value2.Examples?.FirstOrDefault();
				if (text3 != null)
				{
					int num = 1;
					List<JsonNode> list2 = new List<JsonNode>(num);
					CollectionsMarshal.SetCount(list2, num);
					CollectionsMarshal.AsSpan(list2)[0] = text3.Parse();
					schema.Examples = list2;
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(text2))
				{
					schema.Metadata["x-ref-description"] = text2;
				}
				string text4 = value2.Examples?.FirstOrDefault();
				if (text4 != null)
				{
					schema.Metadata["x-ref-example"] = text4.Parse();
				}
			}
		}
		return Task.CompletedTask;
	}
}
