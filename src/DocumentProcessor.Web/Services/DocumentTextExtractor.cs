using System.Text;
using DocumentProcessor.Web.Configuration;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace DocumentProcessor.Web.Services;

public sealed class DocumentTextExtractor(IOptions<BedrockOptions> options)
{
    private readonly BedrockOptions _options = options.Value;

    public static bool CanExtract(string extension) =>
        extension is ".pdf" or ".txt" or ".log";

    public async Task<string> ExtractAsync(
        string fileExtension,
        Stream content,
        CancellationToken cancellationToken = default) => fileExtension switch
        {
            ".pdf" => await Task.Run(() => ExtractFromPdf(content), cancellationToken),
            ".txt" or ".log" => await ExtractFromTextAsync(content, cancellationToken),
            _ => throw new NotSupportedException($"Cannot extract text from '{fileExtension}' files.")
        };

    private string ExtractFromPdf(Stream content)
    {
        var text = new StringBuilder();
        using var pdf = PdfDocument.Open(content);

        foreach (var page in pdf.GetPages().Take(_options.MaxPdfPages))
        {
            text.AppendLine(ContentOrderTextExtractor.GetText(page));
            if (text.Length >= _options.MaxInputCharacters) break;
        }

        return Truncate(text.ToString());
    }

    private async Task<string> ExtractFromTextAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content);
        return Truncate(await reader.ReadToEndAsync(cancellationToken));
    }

    private string Truncate(string text) =>
        text.Length > _options.MaxInputCharacters ? text[.._options.MaxInputCharacters] : text;
}
