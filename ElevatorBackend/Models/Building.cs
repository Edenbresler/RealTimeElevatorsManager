using System.ComponentModel.DataAnnotations.Schema;

namespace ElevatorBackend.Models
{
    public class Building
    {
        public int Id { get; set; } // PK
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
        public string Name { get; set; } = string.Empty;

        public int NumberOfFloors { get; set; }

        public ICollection<ElevatorCall> ElevatorCalls { get; set; } = new List<ElevatorCall>();


        // Navigation property (Bonus)
        public List<Elevator> Elevators { get; set; } = new();
    }
}
