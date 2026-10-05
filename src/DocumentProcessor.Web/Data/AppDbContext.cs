using DocumentProcessor.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessor.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(document =>
        {
            document.HasQueryFilter(d => !d.IsDeleted);
            document.Property(d => d.FileName).HasMaxLength(260);
            document.Property(d => d.OriginalFileName).HasMaxLength(260);
            document.Property(d => d.FileExtension).HasMaxLength(16);
            document.Property(d => d.ContentType).HasMaxLength(128);
            document.Property(d => d.StoragePath).HasMaxLength(512);
            document.Property(d => d.UploadedBy).HasMaxLength(128);
            document.HasIndex(d => d.UploadedAt).IsDescending();
        });
    }
}
