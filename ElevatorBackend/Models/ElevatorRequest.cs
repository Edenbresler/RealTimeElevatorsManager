namespace ElevatorBackend.Models
{
    public class ElevatorRequest
    {
        public int Id { get; set; }
        public int ElevatorId { get; set; }
        public Elevator Elevator { get; set; } = null!;
        public int Floor { get; set; }
        public Direction Direction { get; set; }
        public RequestType Type { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}
