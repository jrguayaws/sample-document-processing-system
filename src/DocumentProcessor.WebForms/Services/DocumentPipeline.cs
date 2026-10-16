using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DocumentProcessor.WebForms.Data;
using DocumentProcessor.WebForms.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessor.WebForms.Services
{
    /// <summary>Extracts text from a stored document and saves an AI-generated summary.</summary>
    public class DocumentPipeline
    {
        private readonly IDbContextFactory<DocumentDbContext> _dbFactory;
        private readonly IDocumentStorage _storage;
        private readonly DocumentTextExtractor _textExtractor;
        private readonly IDocumentSummarizer _summarizer;

        public DocumentPipeline(
            IDbContextFactory<DocumentDbContext> dbFactory,
            IDocumentStorage storage,
            DocumentTextExtractor textExtractor,
            IDocumentSummarizer summarizer)
        {
            _dbFactory = dbFactory;
            _storage = storage;
            _textExtractor = textExtractor;
            _summarizer = summarizer;
        }

        public async Task ProcessAsync(Guid documentId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var document = await db.Documents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

            if (document == null)
            {
                Trace.TraceWarning("Document {0} not found; skipping processing.", documentId);
                return;
            }

            try
            {
                document.Status = DocumentStatus.Processing;
                await db.SaveChangesAsync();

                using (var content = _storage.OpenRead(document.StoragePath))
                {
                    var text = _textExtractor.Extract(document.FileExtension, content);

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        throw new InvalidOperationException(
                            "No text could be extracted from the document.");
                    }

                    document.Summary = _summarizer.Summarize(document.OriginalFileName, text);
                }

                document.Status = DocumentStatus.Processed;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Processing failed for document {0}. {1}", documentId, ex);
                document.Status = DocumentStatus.Failed;
            }

            await db.SaveChangesAsync();
        }
    }
}
