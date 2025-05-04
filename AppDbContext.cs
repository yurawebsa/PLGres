using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CounterPlg {
    public class AppDbContext : DbContext {
        public DbSet<Counter> Counters { get; set; }
        public DbSet<CounterNumber> CounterNumbers { get; set; }
        public DbSet<ActionLogEntity> ActionLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=counter.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Counter>()
                .HasIndex(c => c.CounterNumber)
                .IsUnique();

            modelBuilder.Entity<CounterNumber>()
                .HasIndex(cn => cn.Number)
                .IsUnique();

            modelBuilder.Entity<CounterNumber>()
                .Property(cn => cn.IsWrittenOff)
                .HasDefaultValue(false);

            modelBuilder.Entity<ActionLogEntity>()
                .Property(a => a.Timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}