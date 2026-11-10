using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocumentProcessor.WebForms.Services
{
    /// <summary>Background worker that runs <see cref="DocumentPipeline"/> for queued documents.</summary>
    public class DocumentProcessingService : BackgroundService
    {
        private readonly DocumentProcessingQueue _queue;
        private readonly DocumentPipeline _pipeline;
        private readonly ILogger<DocumentProcessingService> _logger;

        public DocumentProcessingService(
            DocumentProcessingQueue queue,
            DocumentPipeline pipeline,
            ILogger<DocumentProcessingService> logger)
        {
            _queue = queue;
            _pipeline = pipeline;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var documentId in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    var status = await _pipeline.ProcessAsync(documentId, stoppingToken);
                    if (status.HasValue)
                    {
                        _queue.OnStatusChanged(documentId, status.Value);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // The pipeline already marks the document Failed; this catches only
                    // failures before the row could be loaded (e.g. the database is down).
                    _logger.LogError(ex, "Background processing failed for document {DocumentId}.", documentId);
                }
            }
        }
    }
}
