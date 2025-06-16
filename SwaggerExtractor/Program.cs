using System.CommandLine;
using Microsoft.OpenApi.Readers;
using CsvHelper;
using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper.Configuration;

//dotnet run --project SwaggerExtractor -- --input swagger.json --output ApiDocsData.csv

var inputOption = new Option<FileInfo>("--input", "Path to the Swagger JSON file") { IsRequired = true };
var outputOption = new Option<FileInfo>("--output", "Path to save CSV file") { IsRequired = true };

var rootCommand = new RootCommand("Swagger to CSV Extractor");
rootCommand.AddOption(inputOption);
rootCommand.AddOption(outputOption);

rootCommand.SetHandler((FileInfo input, FileInfo output) =>
{
    ExtractSwagger(input.FullName, output.FullName);
}, inputOption, outputOption);

await rootCommand.InvokeAsync(args);

// Helper logic
void ExtractSwagger(string swaggerPath, string outputPath)
{
    var stream = File.OpenRead(swaggerPath);
    var openApiDoc = new OpenApiStreamReader().Read(stream, out var diagnostic);
    var records = new List<ApiDocRecord>();

    foreach (var pathItem in openApiDoc.Paths)
    {
        foreach (var operation in pathItem.Value.Operations)
        {
            var desc = operation.Value.Description ?? "";
            if (!string.IsNullOrWhiteSpace(desc))
                records.Add(new(desc, GuessLabel(desc, pathItem.Key)));

            foreach (var param in operation.Value.Parameters)
            {
                if (!string.IsNullOrWhiteSpace(param.Description))
                    records.Add(new(param.Description, "Parameters"));
            }

            foreach (var response in operation.Value.Responses)
            {
                if (!string.IsNullOrWhiteSpace(response.Value.Description))
                {
                    var label = response.Key.StartsWith("4") ? "Errors" : "Responses";
                    records.Add(new(response.Value.Description, label));
                }
            }
        }
    }

    using var writer = new StreamWriter(outputPath);
    using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
    csv.WriteRecords(records);

    Console.WriteLine($"✅ Extracted {records.Count} records to: {outputPath}");
}

string GuessLabel(string text, string path)
{
    text = text.ToLower();

    if (path.Contains("auth") || text.Contains("token") || text.Contains("api key"))
        return "Authentication";
    if (text.Contains("limit") || text.Contains("offset") || text.Contains("page"))
        return "Pagination";
    if (text.Contains("rate limit") || text.Contains("429"))
        return "Rate limits";
    if (text.Contains("error") || text.Contains("403") || text.Contains("401"))
        return "Errors";

    return "Endpoints";
}

record ApiDocRecord(string Text, string Label);
