using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocumentProcessor.WebForms.Models
{
    [Table("Documents")]
    public class Document
    {
        public Document()
        {
            Id = Guid.NewGuid();
            StoragePath = string.Empty;
            UploadedBy = "System";
            Status = DocumentStatus.Pending;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; }

        [Required]
        [StringLength(260)]
        public string FileName { get; set; }

        [Required]
        [StringLength(260)]
        public string OriginalFileName { get; set; }

        [Required]
        [StringLength(16)]
        public string FileExtension { get; set; }

        public long FileSize { get; set; }

        [Required]
        [StringLength(128)]
        public string ContentType { get; set; }

        [StringLength(512)]
        public string StoragePath { get; set; }

        public DateTimeOffset UploadedAt { get; set; }

        public DocumentStatus Status { get; set; }

        public string Summary { get; set; }

        [StringLength(128)]
        public string UploadedBy { get; set; }

        public bool IsDeleted { get; set; }
    }
}
