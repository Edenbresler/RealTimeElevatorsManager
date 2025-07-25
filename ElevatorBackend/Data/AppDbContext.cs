using ElevatorBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace ElevatorBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Building> Buildings { get; set; }
        public DbSet<Elevator> Elevators { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ElevatorCall> ElevatorCalls { get; set; }
        public DbSet<ElevatorCallAssignment> ElevatorCallAssignments { get; set; }
        public DbSet<ElevatorRequest> ElevatorRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // הגדרה ל-ID של Elevator
            modelBuilder.Entity<Elevator>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // קשר Elevator -> Building
            modelBuilder.Entity<Elevator>()
                .HasOne(e => e.Building)
                .WithMany(b => b.Elevators)
                .HasForeignKey(e => e.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // קשר Building -> User
            modelBuilder.Entity<Building>()
                .HasOne(b => b.User)
                .WithMany(u => u.Buildings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // קשר ElevatorCall -> Building
            modelBuilder.Entity<ElevatorCall>()
                .HasOne(c => c.Building)
                .WithMany(b => b.ElevatorCalls)
                .HasForeignKey(c => c.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // קשר ElevatorCallAssignment -> Elevator
            modelBuilder.Entity<ElevatorCallAssignment>()
                .HasOne(eca => eca.Elevator)
                .WithMany(e => e.ElevatorCallAssignments)
                .HasForeignKey(eca => eca.ElevatorId)
                .OnDelete(DeleteBehavior.Restrict); // 🔁 תיקון לבעיה שלך

            // קשר ElevatorCallAssignment -> ElevatorCall
            modelBuilder.Entity<ElevatorCallAssignment>()
                .HasOne(eca => eca.ElevatorCall)
                .WithMany(ec => ec.ElevatorCallAssignments)
                .HasForeignKey(eca => eca.ElevatorCallId)
                .OnDelete(DeleteBehavior.Cascade);

            // קשר Elevator -> ElevatorRequest (AllRequests)
            modelBuilder.Entity<Elevator>()
                .HasMany(e => e.AllRequests)
                .WithOne(r => r.Elevator)
                .HasForeignKey(r => r.ElevatorId);


        }
    }
}
