using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElevatorBackend.Models
{
    public enum ElevatorStatus
    {
        Idle,
        MovingUp,
        MovingDown,
        OpeningDoors,
        ClosingDoors,
        WaitingForDestination
    }

    public enum Direction
    {
        Up,
        Down,
        None
    }

    public enum DoorStatus
    {
        Open,
        Closed
    }
    public class Elevator
    {
        [Key]
        public int Id { get; set; }

        public int BuildingId { get; set; }
        public Building Building { get; set; } = null!;

        public int CurrentFloor { get; set; } = 0;

        public ElevatorStatus Status { get; set; } = ElevatorStatus.Idle;
        public Direction Direction { get; set; } = Direction.None;
        public DoorStatus DoorStatus { get; set; } = DoorStatus.Closed;

        public ICollection<ElevatorCallAssignment> ElevatorCallAssignments { get; set; } = new List<ElevatorCallAssignment>();

        // Unified list saved in DB
        public ICollection<ElevatorRequest> AllRequests { get; set; } = new List<ElevatorRequest>();


        // Computed subsets (not mapped)
        [NotMapped]
        public IReadOnlyCollection<ElevatorRequest> Requests => AllRequests.Where(r => r.Type == RequestType.Regular).ToList();

        [NotMapped]
        public IReadOnlyCollection<ElevatorRequest> DestinationRequests => AllRequests.Where(r => r.Type == RequestType.Destination).ToList();

        // Add helper
        public void AddRequest(ElevatorRequest request)
        {
            AllRequests.Add(request);
        }

        public void ClearRequestsOfType(RequestType type)
        {
            AllRequests = AllRequests.Where(r => r.Type != type).ToList();
        }
    }
}
