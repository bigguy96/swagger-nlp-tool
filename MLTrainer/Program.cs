using Microsoft.ML;
using Microsoft.ML.Data;

public class ApiDocData
{
    [LoadColumn(0)]
    public string Text { get; set; }

    [LoadColumn(1)]
    public string Label { get; set; }
}

public class ApiDocPrediction
{
    [ColumnName("PredictedLabel")]
    public string PredictedCategory { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        var mlContext = new MLContext();

        string dataPath = "ApiDocsData.csv";

        IDataView dataView = mlContext.Data.LoadFromTextFile<ApiDocData>(dataPath, hasHeader: true, separatorChar: ',');

        var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(mlContext.Transforms.Text.FeaturizeText("Text", "Features"))
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        var model = pipeline.Fit(dataView);

        var predictor = mlContext.Model.CreatePredictionEngine<ApiDocData, ApiDocPrediction>(model);

        var test = new ApiDocData { Text = "Use your bearer token in the Authorization header" };
        var result = predictor.Predict(test);

        Console.WriteLine($"🧠 Prediction: {result.PredictedCategory}");
    }
}
