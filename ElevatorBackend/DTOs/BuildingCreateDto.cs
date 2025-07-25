namespace ElevatorBackend.Dtos
{
    public class BuildingCreateDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public int NumberOfFloors { get; set; }

        
    }
}