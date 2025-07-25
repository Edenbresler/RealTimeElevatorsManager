namespace ElevatorBackend.Models
{
    public class ElevatorDto
    {
        public int Id { get; set; }
        public int BuildingId { get; set; }
        public int CurrentFloor { get; set; }
        public ElevatorStatus Status { get; set; }
        public Direction Direction { get; set; }
        public DoorStatus DoorStatus { get; set; }
    }

}
