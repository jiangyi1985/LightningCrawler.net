using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Common.CrawlerDbContext
{
    partial class CrawlerContext
    {
        public static readonly ILoggerFactory DebugLoggerFactory
            = LoggerFactory.Create(builder => { builder.AddDebug(); });

        public static CrawlerContext Create(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CrawlerContext>();

            optionsBuilder.UseSqlServer(connectionString);
            optionsBuilder.UseLoggerFactory(DebugLoggerFactory);
            optionsBuilder.EnableSensitiveDataLogging();

            var db = new CrawlerContext(optionsBuilder.Options);

            return db;
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Uri>()
                .Property(e => e.CrawledAt)
                .HasConversion(v => v, v => v.HasValue? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc):v);
            modelBuilder.Entity<Uri>()
                .Property(e => e.CreateAt)
                .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
            modelBuilder.Entity<Uri>()
                .Property(e => e.FailedAt)
                .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            modelBuilder.Entity<Relation>()
                .Property(e => e.CreatedAt)
                .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
        }
    }
}
