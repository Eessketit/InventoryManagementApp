using Markdig;

namespace InventoryApp.Services;

/// <summary>Singleton wrapper around Markdig. Registered in DI as singleton.</summary>
public class MarkdownService
{
    private static readonly MarkdownPipeline _pipeline =
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

    public string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;
        return Markdown.ToHtml(markdown, _pipeline);
    }
}
