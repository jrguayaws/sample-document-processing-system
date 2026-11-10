using System.Threading;
using System.Threading.Tasks;

namespace DocumentProcessor.WebForms.Services
{
    public interface IDocumentSummarizer
    {
        Task<string> SummarizeAsync(string fileName, string text, CancellationToken cancellationToken = default);
    }
}
