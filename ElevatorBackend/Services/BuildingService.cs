using ElevatorBackend.Data;
using ElevatorBackend.Models;

namespace ElevatorBackend.Services
{
    public class BuildingService
    {
        private readonly AppDbContext _dbContext;
        private readonly ElevatorService _elevatorService;

        public BuildingService(AppDbContext dbContext, ElevatorService elevatorService)
        {
            _dbContext = dbContext;
            _elevatorService = elevatorService;
        }

        public List<Building> GetAll() =>
            _dbContext.Buildings.ToList();

        public Building? GetById(int id) =>
            _dbContext.Buildings.FirstOrDefault(b => b.Id == id);

        public void Add(Building building)
        {
            
            _dbContext.Buildings.Add(building);
            _dbContext.SaveChanges(); 

            
            var elevator = new Elevator
            {
                BuildingId = building.Id,
                CurrentFloor = 0,
                Status = ElevatorStatus.Idle,
                Direction = Direction.None,
                DoorStatus = DoorStatus.Closed
            };

            _elevatorService.AddElevator(elevator);
            Console.WriteLine($"Created elevator for building {building.Id}");
        }

        public List<Building> GetByUserId(int userId)
        {
            return _dbContext.Buildings.Where(b => b.UserId == userId).ToList();
        }


    }
}
