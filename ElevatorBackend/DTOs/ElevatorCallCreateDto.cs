using ElevatorBackend.Models;

namespace ElevatorBackend.Dtos
{
    public class ElevatorCallCreateDto
    {
        public int BuildingId { get; set; }
        public int RequestedFloor { get; set; }
        public int? DestinationFloor { get; set; }
        public Direction Direction { get; set; }
    }
}