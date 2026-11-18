using ArchitectureAnalyzer.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchitectureAnalyzer.Data
{
    public class ArchitectureAnalyzerContext : DbContext
    {
        public ArchitectureAnalyzerContext(DbContextOptions<ArchitectureAnalyzerContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Project { get; set; }
        public DbSet<CodeClass> CodeClass { get; set; }
        public DbSet<Dependency> Dependency { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CodeClass>()
                .HasOne<Project>()
                .WithMany(p => p.Classes)
                .HasForeignKey(c => c.ProjectID)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Dependency>()
                .HasOne(d => d.SourceClass)
                .WithMany()
                .HasForeignKey(d => d.SourceClassId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Dependency>()
                .HasOne(d => d.TargetClass)
                .WithMany()
                .HasForeignKey(d => d.TargetClassId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
