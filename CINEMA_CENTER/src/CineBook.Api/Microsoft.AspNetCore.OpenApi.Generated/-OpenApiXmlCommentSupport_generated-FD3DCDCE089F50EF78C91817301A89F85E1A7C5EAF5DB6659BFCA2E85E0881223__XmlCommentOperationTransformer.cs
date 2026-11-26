using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.OpenApi;

namespace Microsoft.AspNetCore.OpenApi.Generated;

[GeneratedCode("Microsoft.AspNetCore.OpenApi.SourceGenerators, Version=10.0.12.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "10.0.12.0")]
internal class _003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlCommentOperationTransformer : IOpenApiOperationTransformer
{
	public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
	{
		MethodInfo methodInfo = ((context.Description.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor) ? controllerActionDescriptor.MethodInfo : context.Description.ActionDescriptor.EndpointMetadata.OfType<MethodInfo>().SingleOrDefault());
		if ((object)methodInfo == null)
		{
			return Task.CompletedTask;
		}
		if (_003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlCommentCache.Cache.TryGetValue(_003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__DocumentationCommentIdHelper.NormalizeDocId(methodInfo.CreateDocumentationId()), out _003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlComment value))
		{
			string summary = value.Summary;
			if (summary != null)
			{
				operation.Summary = summary;
			}
			string description = value.Description;
			if (description != null)
			{
				operation.Description = description;
			}
			string remarks = value.Remarks;
			if (remarks != null)
			{
				operation.Description = remarks;
			}
			List<_003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlParameterComment> parameters = value.Parameters;
			if (parameters != null && parameters.Count > 0)
			{
				foreach (_003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlParameterComment parameterComment in value.Parameters)
				{
					ParameterInfo parameterInfo = methodInfo.GetParameters().SingleOrDefault((ParameterInfo info) => info.Name == parameterComment.Name);
					IOpenApiParameter openApiParameter = operation.Parameters?.SingleOrDefault((IOpenApiParameter parameter) => parameter.Name == parameterComment.Name);
					if (openApiParameter != null)
					{
						OpenApiParameter openApiParameter2 = UnwrapOpenApiParameter(openApiParameter);
						openApiParameter2.Description = parameterComment.Description;
						string example = parameterComment.Example;
						if (example != null)
						{
							openApiParameter2.Example = example.Parse();
						}
						openApiParameter2.Deprecated = parameterComment.Deprecated;
						continue;
					}
					IOpenApiRequestBody requestBody = operation.RequestBody;
					if (requestBody == null)
					{
						continue;
					}
					requestBody.Description = parameterComment.Description;
					string example2 = parameterComment.Example;
					if (example2 == null)
					{
						continue;
					}
					ICollection<OpenApiMediaType> collection = requestBody?.Content?.Values;
					if (collection == null)
					{
						continue;
					}
					foreach (OpenApiMediaType item in collection)
					{
						item.Example = example2.Parse();
					}
				}
			}
			string returns = value.Returns;
			if (returns != null)
			{
				OpenApiResponses responses = operation.Responses;
				if (responses != null && responses.Count == 1)
				{
					operation.Responses.First().Value.Description = returns;
				}
			}
			List<_003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlResponseComment> responses2 = value.Responses;
			if (responses2 != null && responses2.Count > 0)
			{
				OpenApiResponses responses = operation.Responses;
				if (responses != null && responses.Count > 0)
				{
					foreach (KeyValuePair<string, IOpenApiResponse> response in operation.Responses)
					{
						_003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlResponseComment obj = value.Responses.SingleOrDefault((_003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlResponseComment xmlResponse) => xmlResponse.Code == response.Key);
						if ((object)obj != null)
						{
							response.Value.Description = obj.Description;
						}
					}
				}
			}
		}
		foreach (ApiParameterDescription parameterDescription in context.Description.ParameterDescriptions)
		{
			ModelMetadata metadata = parameterDescription.ModelMetadata;
			if (metadata == null || metadata.MetadataKind != ModelMetadataKind.Property)
			{
				continue;
			}
			Type containerType = metadata.ContainerType;
			if ((object)containerType == null)
			{
				continue;
			}
			string propertyName = metadata.PropertyName;
			if (propertyName == null)
			{
				continue;
			}
			string docId = _003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__DocumentationCommentIdHelper.CreateDocumentationId(containerType, propertyName);
			if (!_003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlCommentCache.Cache.TryGetValue(_003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__DocumentationCommentIdHelper.NormalizeDocId(docId), out _003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlComment value2))
			{
				continue;
			}
			IOpenApiParameter openApiParameter3 = operation.Parameters?.SingleOrDefault((IOpenApiParameter p) => p.Name == metadata.Name);
			string text = value2.Summary;
			if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(value2.Value))
			{
				text = text + "\n" + value2.Value;
			}
			else if (string.IsNullOrEmpty(text))
			{
				text = value2.Value;
			}
			if (openApiParameter3 == null)
			{
				if (operation.RequestBody == null)
				{
					continue;
				}
				operation.RequestBody.Description = text;
				string text2 = value2.Examples?.FirstOrDefault();
				if (text2 == null)
				{
					continue;
				}
				ICollection<OpenApiMediaType> collection2 = operation.RequestBody.Content?.Values;
				if (collection2 == null)
				{
					continue;
				}
				JsonNode example3 = text2.Parse();
				foreach (OpenApiMediaType item2 in collection2)
				{
					item2.Example = example3;
				}
				continue;
			}
			OpenApiParameter openApiParameter4 = UnwrapOpenApiParameter(openApiParameter3);
			if (openApiParameter4 != null)
			{
				openApiParameter4.Description = text;
				string text3 = value2.Examples?.FirstOrDefault();
				if (text3 != null)
				{
					openApiParameter4.Example = text3.Parse();
				}
			}
		}
		return Task.CompletedTask;
	}

	private static OpenApiParameter UnwrapOpenApiParameter(IOpenApiParameter sourceParameter)
	{
		if (sourceParameter is OpenApiParameterReference openApiParameterReference)
		{
			if (openApiParameterReference.Target is OpenApiParameter result)
			{
				return result;
			}
			throw new InvalidOperationException("The input schema must be an OpenApiParameter or OpenApiParameterReference.");
		}
		if (sourceParameter is OpenApiParameter result2)
		{
			return result2;
		}
		throw new InvalidOperationException("The input schema must be an OpenApiParameter or OpenApiParameterReference.");
	}
}
