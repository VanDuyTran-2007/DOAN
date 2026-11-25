using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.AspNetCore.OpenApi.Generated;

[GeneratedCode("Microsoft.AspNetCore.OpenApi.SourceGenerators, Version=10.0.12.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "10.0.12.0")]
internal record _003COpenApiXmlCommentSupport_generated_003EFD3DCDCE089F50EF78C91817301A89F85E1A7C5EAF5DB6659BFCA2E85E0881223__XmlParameterComment(string? Name, string? Description, string? Example, bool Deprecated)
{
	[CompilerGenerated]
	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("XmlParameterComment");
		stringBuilder.Append(" { ");
		if (PrintMembers(stringBuilder))
		{
			stringBuilder.Append(' ');
		}
		stringBuilder.Append('}');
		return stringBuilder.ToString();
	}
}
