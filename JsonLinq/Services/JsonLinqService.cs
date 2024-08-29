namespace JsonLinq.Services;

using System;
using System.IO;
using System.Reflection;
using System.Text;
using JsonLinq.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json.Linq;

internal sealed class JsonLinqService
{
	public (string Json, int Count) Process(string json, string query)
	{
		if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(query)) return (json, 0);

		// Test JSON is parseable
		JArray jArray;
		try
		{
			jArray = JArray.Parse(json);
		}
		catch (Exception ex)
		{
			return (ex.Message, 0);
		}

		var sb = new StringBuilder();
		sb.AppendLine("using Newtonsoft.Json;");
		sb.AppendLine("using Newtonsoft.Json.Linq;");
		sb.AppendLine("using System.Linq;");
		sb.AppendLine("using JsonLinq.Interfaces;");
		sb.AppendLine("public class Action : ICompiledAction");
		sb.AppendLine("{");

		sb.AppendLine("public string Run()");
		sb.AppendLine("{");
		sb.AppendLine($""""
			var json = """
			{json}
			""";
			"""");
		sb.AppendLine("""			
			var jObj = JArray.Parse(json);
			""");
		sb.AppendLine($"""
			var result = jObj.{query.TrimStart('.').Trim()};
			""");
		sb.AppendLine($"""
			System.Console.Write(result);
			""");
		sb.AppendLine($"""
			return JsonConvert.SerializeObject(result, Formatting.Indented);
			""");
		sb.AppendLine("}");

		sb.AppendLine("}");

		string? jsonResult = null;
		try
		{
			jsonResult = CompileAndRun(sb.ToString());
			jArray = JArray.Parse(jsonResult);

			return (jsonResult, jArray.Count);
		}
		catch (Exception ex)
		{
			return (string.IsNullOrWhiteSpace(jsonResult) ? ex.Message : "Error: " + jsonResult, 0);
		}
	}

	private string CompileAndRun(string input)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(input);

		IEnumerable<MetadataReference> metadataReferences = RuntimeMetadataReference.DefaultAssemblyNames
			.Select(f => MetadataReference.CreateFromFile(RuntimeMetadataReference.TrustedPlatformAssemblyMap[f]))
			.Append(RuntimeMetadataReference.CorLibReference)
			.Append(MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location))
			.Append(MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly.Location))
			.Append(MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location))
			.Append(MetadataReference.CreateFromFile(typeof(System.Text.Json.Serialization.JsonIgnoreCondition).Assembly.Location))
			.Append(MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location))
			.Append(MetadataReference.CreateFromFile(typeof(JsonLinq.Interfaces.ICompiledAction).Assembly.Location))
			.Append(MetadataReference.CreateFromFile(typeof(Newtonsoft.Json.Linq.JObject).Assembly.Location))
			.Append(MetadataReference.CreateFromFile(typeof(Newtonsoft.Json.JsonConverter).Assembly.Location));

		CSharpCompilation compilation =
			CSharpCompilation.Create(
				"TempLib",
				[syntaxTree],
				metadataReferences,
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempLib.dll");
		if (File.Exists(path))
		{
			File.Delete(path);
		}

		using (var dllStream = File.OpenWrite(path))
		{
			var emitResult = compilation.Emit(dllStream);
			if (!emitResult.Success)
			{
				return string.Join(Environment.NewLine, emitResult.Diagnostics.Select(e => e.ToString()));
			}
		}

		var ass = Assembly.Load(File.ReadAllBytes(path));
		var @type = ass.GetType("Action") ?? throw new Exception();
		if (Activator.CreateInstance(@type) is not ICompiledAction instance) throw new Exception();
		try
		{
			var result = instance.Run();
			return result;
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}
}
