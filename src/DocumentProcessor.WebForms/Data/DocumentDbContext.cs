using Microsoft.EntityFrameworkCore;
using DocumentProcessor.WebForms.Models;

namespace DocumentProcessor.WebForms.Data
{
    public class DocumentDbContext : DbContext
    {
        // The schema is created by App_Data/Schema.sql at application start. EF Core never
        // creates the database or checks a model hash unless EnsureCreated/Migrate is called,
        // and neither is called anywhere, so the EF6 Database.SetInitializer(null) has no equivalent.

        public DocumentDbContext(DbContextOptions<DocumentDbContext> options)
            : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Soft delete: rows flagged IsDeleted are excluded from every query.
            // Use IgnoreQueryFilters() where deleted rows are deliberately needed.
            modelBuilder.Entity<Document>().HasQueryFilter(d => !d.IsDeleted);
        }
    }
}
