namespace DocumentProcessor.Web.Models;

public enum DocumentStatus
{
    Pending,
    Processing,
    Processed,
    Failed
}

public class Document
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string FileName { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FileExtension { get; set; }
    public long FileSize { get; set; }
    public required string ContentType { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public DocumentStatus Status { get; set; }
    public string? Summary { get; set; }
    public string UploadedBy { get; set; } = "System";
    public bool IsDeleted { get; set; }
}
