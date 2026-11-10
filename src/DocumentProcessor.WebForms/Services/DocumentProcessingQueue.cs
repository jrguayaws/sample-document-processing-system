using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DocumentProcessor.WebForms.Models;

namespace DocumentProcessor.WebForms.Services
{
    /// <summary>
    /// In-memory queue of documents waiting for <see cref="DocumentPipeline"/>. Uploads enqueue
    /// the stored document id and return immediately, so the Blazor circuit is not blocked
    /// while Bedrock answers; <see cref="DocumentProcessingService"/> drains the queue.
    /// The document row itself carries Pending/Processing/Processed/Failed for the list.
    /// </summary>
    public class DocumentProcessingQueue
    {
        private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        /// <summary>
        /// Raised (on a background thread) whenever a queued document changes status, so a
        /// component can refresh its list. Handlers must marshal with InvokeAsync.
        /// </summary>
        public event Action<Guid, DocumentStatus> StatusChanged;

        public ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(documentId, cancellationToken);
        }

        internal ChannelReader<Guid> Reader
        {
            get { return _channel.Reader; }
        }

        internal void OnStatusChanged(Guid documentId, DocumentStatus status)
        {
            StatusChanged?.Invoke(documentId, status);
        }
    }
}
