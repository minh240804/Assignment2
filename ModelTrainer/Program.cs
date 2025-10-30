using Microsoft.ML;
using Assignment2.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// 1. Định nghĩa cấu trúc dữ liệu cho model
public class ArticleInput
{
    public string ArticleId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ArticleOutput
{
    public float[] Features { get; set; } = Array.Empty<float>();
}

class Program
{
    static void Main(string[] args)
    {
        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        var mlContext = new MLContext();

        // 2. Tải dữ liệu từ database
        Console.WriteLine("Loading data from database...");
        
        var optionsBuilder = new DbContextOptionsBuilder<FunewsManagementContext>();
        optionsBuilder.UseSqlServer(connectionString);
        
        using var dbContext = new FunewsManagementContext(optionsBuilder.Options);
        
        var allArticles = dbContext.NewsArticles
            .Where(a => a.NewsStatus == true) // Chỉ lấy bài viết đã publish
            .Select(a => new ArticleInput
            {
                ArticleId = a.NewsArticleId,
                Content = a.NewsTitle + " " + a.NewsContent + " " + a.Headline
            })
            .ToList();

        Console.WriteLine($"Loaded {allArticles.Count} articles");

        var dataView = mlContext.Data.LoadFromEnumerable(allArticles);

        // 3. Xây dựng pipeline xử lý dữ liệu
        var pipeline = mlContext.Transforms.Text
            .FeaturizeText(
                outputColumnName: "Features", 
                inputColumnName: nameof(ArticleInput.Content));

        // 4. Huấn luyện model
        Console.WriteLine("Training model...");
        var model = pipeline.Fit(dataView);

        // 5. Lưu model ra file .zip
        var modelPath = Path.Combine(
            Directory.GetCurrentDirectory(), 
            "ArticleSuggestionModel.zip");
            
        mlContext.Model.Save(model, dataView.Schema, modelPath);

        Console.WriteLine($"Model saved to: {modelPath}");
        Console.WriteLine("Training completed successfully!");
    }
}