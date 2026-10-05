using DocumentProcessor.Web.Data;
using DocumentProcessor.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessor.Web.Services;

/// <summary>Extracts text from a stored document and saves an AI-generated summary.</summary>
public sealed class DocumentPipeline(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IDocumentStorage storage,
    DocumentTextExtractor textExtractor,
    IDocumentSummarizer summarizer,
    ILogger<DocumentPipeline> logger)
{
    public async Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document is null)
        {
            logger.LogWarning("Document {DocumentId} not found; skipping processing.", documentId);
            return;
        }

        try
        {
            document.Status = DocumentStatus.Processing;
            await db.SaveChangesAsync(cancellationToken);

            await using var content = storage.OpenRead(document.StoragePath);
            var text = await textExtractor.ExtractAsync(document.FileExtension, content, cancellationToken);

            document.Summary = await summarizer.SummarizeAsync(document.OriginalFileName, text, cancellationToken);
            document.Status = DocumentStatus.Processed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Processing failed for document {DocumentId}.", documentId);
            document.Status = DocumentStatus.Failed;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
