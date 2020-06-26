using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Common.CrawlerDbContext
{
    public partial class CrawlerContext : DbContext
    {
        public CrawlerContext()
        {
        }

        public CrawlerContext(DbContextOptions<CrawlerContext> options)
            : base(options)
        {
        }

        public virtual DbSet<RedirectRelation> RedirectRelation { get; set; }
        public virtual DbSet<Relation> Relation { get; set; }
        public virtual DbSet<Uri> Uri { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer("Server=.\\;Database=Crawler;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RedirectRelation>(entity =>
            {
                entity.HasKey(e => new { e.SourceId, e.DestinationId });

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<Relation>(entity =>
            {
                entity.HasKey(e => new { e.ParentId, e.ChildId });

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<Uri>(entity =>
            {
                entity.Property(e => e.BrowserContent).HasColumnType("ntext");

                entity.Property(e => e.BrowserCrawledAt).HasColumnType("datetime");

                entity.Property(e => e.BrowserFailedAt).HasColumnType("datetime");

                entity.Property(e => e.Content).HasColumnType("ntext");

                entity.Property(e => e.CrawledAt).HasColumnType("datetime");

                entity.Property(e => e.CreateAt).HasColumnType("datetime");

                entity.Property(e => e.FailedAt).HasColumnType("datetime");

                entity.Property(e => e.Host).HasMaxLength(200);

                entity.Property(e => e.Scheme).HasMaxLength(50);

                entity.Property(e => e.StatusCodeString).HasMaxLength(50);

                entity.Property(e => e.TimeTaken).HasColumnType("decimal(18, 8)");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
