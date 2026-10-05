namespace DocumentProcessor.Web.Services;

public interface IDocumentSummarizer
{
    Task<string> SummarizeAsync(string fileName, string text, CancellationToken cancellationToken = default);
}
