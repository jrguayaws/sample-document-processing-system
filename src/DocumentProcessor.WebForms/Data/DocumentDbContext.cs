using Microsoft.EntityFrameworkCore;
using DocumentProcessor.WebForms.Models;

namespace DocumentProcessor.WebForms.Data
{
    public class DocumentDbContext : DbContext
    {
        public DocumentDbContext(DbContextOptions<DocumentDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Global query filter for the soft-delete pattern (replaces per-query !d.IsDeleted checks).
            modelBuilder.Entity<Document>().HasQueryFilter(d => !d.IsDeleted);
        }

        public DbSet<Document> Documents { get; set; }
    }
}
