namespace DocumentProcessor.WebForms.Services
{
    public interface IDocumentSummarizer
    {
        string Summarize(string fileName, string text);
    }
}
