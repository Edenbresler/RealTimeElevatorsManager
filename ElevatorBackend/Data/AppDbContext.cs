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

          
            modelBuilder.Entity<Elevator>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

         
            modelBuilder.Entity<Elevator>()
                .HasOne(e => e.Building)
                .WithMany(b => b.Elevators)
                .HasForeignKey(e => e.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

       
            modelBuilder.Entity<Building>()
                .HasOne(b => b.User)
                .WithMany(u => u.Buildings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

          
            modelBuilder.Entity<ElevatorCall>()
                .HasOne(c => c.Building)
                .WithMany(b => b.ElevatorCalls)
                .HasForeignKey(c => c.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

           
            modelBuilder.Entity<ElevatorCallAssignment>()
                .HasOne(eca => eca.Elevator)
                .WithMany(e => e.ElevatorCallAssignments)
                .HasForeignKey(eca => eca.ElevatorId)
                .OnDelete(DeleteBehavior.Restrict); 

          
            modelBuilder.Entity<ElevatorCallAssignment>()
                .HasOne(eca => eca.ElevatorCall)
                .WithMany(ec => ec.ElevatorCallAssignments)
                .HasForeignKey(eca => eca.ElevatorCallId)
                .OnDelete(DeleteBehavior.Cascade);

            
            modelBuilder.Entity<Elevator>()
                .HasMany(e => e.AllRequests)
                .WithOne(r => r.Elevator)
                .HasForeignKey(r => r.ElevatorId);


        }
    }
}
