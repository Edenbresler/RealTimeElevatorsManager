using ElevatorBackend.Data;
using ElevatorBackend.Hubs;
using ElevatorBackend.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

public class ElevatorService
{
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<ElevatorHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public ElevatorService(AppDbContext dbContext, IHubContext<ElevatorHub> hubContext, IServiceScopeFactory scopeFactory)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
    }

    public List<Elevator> GetAll()
    {
        return _dbContext.Elevators.Include(e => e.AllRequests).ToList();
    }

    public Elevator? GetById(int id)
    {
        return _dbContext.Elevators.Include(e => e.AllRequests).FirstOrDefault(e => e.Id == id);
    }

    public void AddElevator(Elevator elevator)
    {
        elevator.CurrentFloor = 0;
        elevator.Status = ElevatorStatus.Idle;
        elevator.Direction = Direction.None;
        elevator.DoorStatus = DoorStatus.Closed;

        _dbContext.Elevators.Add(elevator);
        _dbContext.SaveChanges();
    }

    public async Task MoveToFloor(int elevatorId)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var elevator = dbContext.Elevators
            .Include(e => e.AllRequests)
            .FirstOrDefault(e => e.Id == elevatorId);

        if (elevator == null)
            return;
        Console.WriteLine($"[Check] Requests: {elevator.Requests.Count}, Destination: {elevator.DestinationRequests.Count}");

        while (elevator.Requests.Any() || elevator.DestinationRequests.Any())
        {
            int? targetFloor = elevator.Requests.Any()
                ? elevator.Requests.First().Floor
                : elevator.DestinationRequests.First().Floor;

            elevator.Direction = targetFloor > elevator.CurrentFloor ? Direction.Up : Direction.Down;
            elevator.Status = elevator.Direction == Direction.Up ? ElevatorStatus.MovingUp : ElevatorStatus.MovingDown;

            elevator.CurrentFloor += elevator.Direction == Direction.Up ? 1 : -1;

            Console.WriteLine($"[Move] Elevator {elevator.Id} moved to floor {elevator.CurrentFloor}");

            await _hubContext.Clients.All.SendAsync("ElevatorUpdated", elevator);
            await Task.Delay(200);

            bool shouldStop = false;

            var regularToRemove = elevator.AllRequests
                .FirstOrDefault(r => r.Floor == elevator.CurrentFloor && r.Type == RequestType.Regular);

            if (regularToRemove != null)
            {
                elevator.AllRequests.Remove(regularToRemove);
                dbContext.ElevatorRequests.Remove(regularToRemove);

                shouldStop = true;
            }

            // Destination request
            var destinationToRemove = elevator.AllRequests
                .FirstOrDefault(r => r.Floor == elevator.CurrentFloor && r.Type == RequestType.Destination);

            if (destinationToRemove != null)
            {
                elevator.AllRequests.Remove(destinationToRemove);
                dbContext.ElevatorRequests.Remove(destinationToRemove);
                shouldStop = true;
            }

            if (shouldStop)
            {
                OpenAndCloseDoors(elevator);
                await _hubContext.Clients.All.SendAsync("ElevatorUpdated", elevator);
            }

            await dbContext.SaveChangesAsync();

        }

        elevator.Status = ElevatorStatus.Idle;
        elevator.Direction = Direction.None;
        _dbContext.SaveChanges();
        await _hubContext.Clients.All.SendAsync("ElevatorUpdated", elevator);
    }

    private void OpenAndCloseDoors(Elevator elevator)
    {
        elevator.Status = ElevatorStatus.OpeningDoors;
        elevator.DoorStatus = DoorStatus.Open;
        Thread.Sleep(300);

        elevator.DoorStatus = DoorStatus.Closed;
        elevator.Status = ElevatorStatus.ClosingDoors;
        Thread.Sleep(300);
    }

    public void RequestElevator(int elevatorId, int floor)
    {
        var elevator = _dbContext.Elevators.Include(e => e.AllRequests).FirstOrDefault(e => e.Id == elevatorId);
        if (elevator == null) return;

        var direction = floor > elevator.CurrentFloor ? Direction.Up :
                        floor < elevator.CurrentFloor ? Direction.Down : Direction.None;

        // Check if elevator can handle the request now
        if (elevator.Status == ElevatorStatus.Idle ||
            (elevator.Direction == Direction.Up && floor >= elevator.CurrentFloor) ||
            (elevator.Direction == Direction.Down && floor <= elevator.CurrentFloor))
        {
            if (!elevator.Requests.Any(r => r.Floor == floor))
            {
                elevator.AddRequest(new ElevatorRequest
                {
                    Floor = floor,
                    Direction = direction,
                    Type = RequestType.Regular
                });
            }
        }
        else
        {
            // במקום לשמור ב-PendingRequests, זה יטופל דרך ElevatorCalls (ראה שינויים עתידיים)
        }

        _dbContext.SaveChanges();

        if (elevator.Status == ElevatorStatus.Idle)
        {
            _ = Task.Run(() => MoveToFloor(elevatorId));
        }
    }

    public void SelectFloor(int elevatorId, int floor)
    {
        var elevator = _dbContext.Elevators.Include(e => e.AllRequests).FirstOrDefault(e => e.Id == elevatorId);
        if (elevator == null) return;

        if (!elevator.DestinationRequests.Any(r => r.Floor == floor))
        {
            var direction = floor > elevator.CurrentFloor ? Direction.Up :
                            floor < elevator.CurrentFloor ? Direction.Down : Direction.None;

            elevator.AddRequest(new ElevatorRequest
            {
                Floor = floor,
                Direction = direction,
                Type = RequestType.Destination
            });
        }

        _dbContext.SaveChanges();

        if (elevator.Status == ElevatorStatus.Idle)
        {
            _ = Task.Run(() => MoveToFloor(elevatorId));
        }
    }

    public List<Elevator> GetElevatorsWithRequests()
    {
        return _dbContext.Elevators
            .Include(e => e.AllRequests)
            .ToList()
            .Where(e => e.Requests.Any() || e.DestinationRequests.Any())
            .ToList();
    }
}
