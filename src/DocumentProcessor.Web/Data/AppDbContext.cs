using DocumentProcessor.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DocumentProcessor.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(document =>
        {
            document.ToTable("documents", "dps_dbo");

            document.Property(d => d.Id).HasColumnName("id");
            document.Property(d => d.FileName).HasColumnName("filename").HasMaxLength(260);
            document.Property(d => d.OriginalFileName).HasColumnName("originalfilename").HasMaxLength(260);
            document.Property(d => d.FileExtension).HasColumnName("fileextension").HasMaxLength(16);
            document.Property(d => d.FileSize).HasColumnName("filesize");
            document.Property(d => d.ContentType).HasColumnName("contenttype").HasMaxLength(128);
            document.Property(d => d.StoragePath).HasColumnName("storagepath").HasMaxLength(512);
            document.Property(d => d.UploadedAt).HasColumnName("uploadedat");
            document.Property(d => d.Status).HasColumnName("status");
            document.Property(d => d.Summary).HasColumnName("summary");
            document.Property(d => d.UploadedBy).HasColumnName("uploadedby").HasMaxLength(128);
            document.Property(d => d.IsDeleted)
                .HasColumnName("isdeleted")
                .HasColumnType("numeric(1,0)")
                .HasConversion(new BoolToZeroOneConverter<decimal>());

            document.HasQueryFilter(d => !d.IsDeleted);
            document.HasIndex(d => d.UploadedAt).IsDescending();
        });
    }
}
