using System;
using System.Threading;
using System.Threading.Tasks;
using DocumentProcessor.WebForms.Data;
using DocumentProcessor.WebForms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentProcessor.WebForms.Services
{
    /// <summary>Extracts text from a stored document and saves an AI-generated summary.</summary>
    /// <remarks>
    /// Registered as a singleton. A DbContext is not thread safe, so each call creates its own
    /// short-lived context from the singleton <see cref="IDbContextFactory{TContext}"/>.
    /// </remarks>
    public class DocumentPipeline
    {
        private readonly IDocumentStorage _storage;
        private readonly DocumentTextExtractor _textExtractor;
        private readonly IDocumentSummarizer _summarizer;
        private readonly IDbContextFactory<DocumentDbContext> _dbFactory;
        private readonly ILogger<DocumentPipeline> _logger;

        public DocumentPipeline(
            IDocumentStorage storage,
            DocumentTextExtractor textExtractor,
            IDocumentSummarizer summarizer,
            IDbContextFactory<DocumentDbContext> dbFactory,
            ILogger<DocumentPipeline> logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _textExtractor = textExtractor ?? throw new ArgumentNullException(nameof(textExtractor));
            _summarizer = summarizer ?? throw new ArgumentNullException(nameof(summarizer));
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Processes the document and returns its final status, or null when it no longer exists.</summary>
        public async Task<DocumentStatus?> ProcessAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            // The global query filter in DocumentDbContext excludes soft-deleted rows.
            var document = await db.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken)
                .ConfigureAwait(false);

            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found; skipping processing.", documentId);
                return null;
            }

            try
            {
                document.Status = DocumentStatus.Processing;
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                using (var content = _storage.OpenRead(document.StoragePath))
                {
                    var text = _textExtractor.Extract(document.FileExtension, content);

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        throw new InvalidOperationException(
                            "No text could be extracted from the document.");
                    }

                    document.Summary = await _summarizer
                        .SummarizeAsync(document.OriginalFileName, text, cancellationToken)
                        .ConfigureAwait(false);
                }

                document.Status = DocumentStatus.Processed;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Processing failed for document {DocumentId}.", documentId);
                document.Status = DocumentStatus.Failed;
            }

            await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            return document.Status;
        }
    }
}
