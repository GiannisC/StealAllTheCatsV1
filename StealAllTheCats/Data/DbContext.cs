using Microsoft.EntityFrameworkCore;
using StealAllTheCats.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace StealAllTheCats.Data
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options) { }

        public DbSet<CatEntity> Cats { get; set; }
        public DbSet<TagEntity> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CatEntity>()
                .HasIndex(c => c.CatId)
                .IsUnique();

            modelBuilder.Entity<CatEntity>()
                .HasMany(c => c.Tags)
                .WithMany(t => t.Cats);
        }
    }
}
