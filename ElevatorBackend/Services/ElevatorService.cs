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
        int? lastCallId = null;

        Console.WriteLine($"[Check] Requests: {elevator.Requests.Count}, Destination: {elevator.DestinationRequests.Count}");

        while (elevator.Requests.Any() || elevator.DestinationRequests.Any())
        {
            int? targetFloor = elevator.Requests.Any()
                ? elevator.Requests.First().Floor
                : elevator.DestinationRequests.First().Floor;

            // חישוב כיוון
            if (targetFloor > elevator.CurrentFloor)
            {
                elevator.Direction = Direction.Up;
                elevator.Status = ElevatorStatus.MovingUp;
            }
            else if (targetFloor < elevator.CurrentFloor)
            {
                elevator.Direction = Direction.Down;
                elevator.Status = ElevatorStatus.MovingDown;
            }
            else // ❗ elevator already at the target floor
            {
                Console.WriteLine($"[Move] Elevator {elevator.Id} already at floor {elevator.CurrentFloor}");


                lastCallId = _dbContext.ElevatorCallAssignments
                    .Where(a => a.ElevatorId == elevator.Id)
                    .OrderByDescending(a => a.AssignmentTime)
                    .Select(a => (int?)a.ElevatorCallId)
                    .FirstOrDefault();




                await _hubContext.Clients.All.SendAsync("ElevatorUpdated", new ElevatorStatusDto
                {
                    ElevatorId = elevator.Id,
                    CurrentFloor = elevator.CurrentFloor,
                    Direction = elevator.Direction.ToString(),
                    Status = elevator.Status.ToString(),
                    DoorStatus = elevator.DoorStatus.ToString(),
                    LastCallId = lastCallId
                });

                await Task.Delay(500); // זמן פתיחה לצורך הדמיה


                // לאחר הפתיחה: הסרת הבקשה שנמצאת בקומה הזו
                var requestToRemove = elevator.AllRequests
                    .FirstOrDefault(r => r.Floor == elevator.CurrentFloor && r.Type == RequestType.Regular);

                if (requestToRemove != null)
                {
                    elevator.AllRequests.Remove(requestToRemove);
                    dbContext.ElevatorRequests.Remove(requestToRemove);
                }

                elevator.Status = ElevatorStatus.WaitingForDestination;
                Console.WriteLine($"[SERVER] Elevator {elevator.Id} status → WaitingForDestination");
                await Task.Delay(1500);

                await dbContext.SaveChangesAsync();

                lastCallId = _dbContext.ElevatorCallAssignments
                    .Where(a => a.ElevatorId == elevator.Id)
                    .OrderByDescending(a => a.AssignmentTime)
                    .Select(a => (int?)a.ElevatorCallId)
                    .FirstOrDefault();


                await _hubContext.Clients.All.SendAsync("ElevatorUpdated", new ElevatorStatusDto
                {
                    ElevatorId = elevator.Id,
                    CurrentFloor = elevator.CurrentFloor,
                    Direction = elevator.Direction.ToString(),
                    Status = elevator.Status.ToString(),
                    DoorStatus = elevator.DoorStatus.ToString(),
                    LastCallId = lastCallId
                });

                break; // נחכה להזנת יעד מהנוסע – לא ממשיכים תנועה בשלב הזה
            }

            // תזוזה בפועל
            elevator.CurrentFloor += elevator.Direction == Direction.Up ? 1 : -1;

            lastCallId = _dbContext.ElevatorCallAssignments
                .Where(a => a.ElevatorId == elevator.Id)
                .OrderByDescending(a => a.AssignmentTime)
                .Select(a => (int?)a.ElevatorCallId)
                .FirstOrDefault();


            await _hubContext.Clients.All.SendAsync("ElevatorUpdated", new ElevatorStatusDto
            {
                ElevatorId = elevator.Id,
                CurrentFloor = elevator.CurrentFloor,
                Direction = elevator.Direction.ToString(),
                Status = elevator.Status.ToString(),
                DoorStatus = elevator.DoorStatus.ToString(),
                LastCallId = lastCallId
            });

            await Task.Delay(2000);

            bool shouldStop = false;

            var regularToRemove = elevator.AllRequests
                .FirstOrDefault(r => r.Floor == elevator.CurrentFloor && r.Type == RequestType.Regular);

            if (regularToRemove != null)
            {
                elevator.AllRequests.Remove(regularToRemove);
                dbContext.ElevatorRequests.Remove(regularToRemove);
                shouldStop = true;
            }

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
                OpenAndChooseFloor(elevator);

                lastCallId = _dbContext.ElevatorCallAssignments
                    .Where(a => a.ElevatorId == elevator.Id)
                    .OrderByDescending(a => a.AssignmentTime)
                    .Select(a => (int?)a.ElevatorCallId)
                    .FirstOrDefault();


                await _hubContext.Clients.All.SendAsync("ElevatorUpdated", new ElevatorStatusDto
                {
                    ElevatorId = elevator.Id,
                    CurrentFloor = elevator.CurrentFloor,
                    Direction = elevator.Direction.ToString(),
                    Status = elevator.Status.ToString(),
                    DoorStatus = elevator.DoorStatus.ToString(),
                    LastCallId = lastCallId
                });
            }
            await Task.Delay(1500);
            await dbContext.SaveChangesAsync();
        }

        if (elevator.Status != ElevatorStatus.WaitingForDestination)
        {
            elevator.Status = ElevatorStatus.Idle;
            elevator.Direction = Direction.None;
            await dbContext.SaveChangesAsync();
        }
        Console.WriteLine($"[SignalR] Sending Elevator Update: ID={elevator.Id}, Status={elevator.Status}, StatusStr={elevator.Status.ToString()}");
        Console.WriteLine($"[SERVER] Final status for elevator {elevator.Id}: {elevator.Status}");

         lastCallId = _dbContext.ElevatorCallAssignments
            .Where(a => a.ElevatorId == elevator.Id)
            .OrderByDescending(a => a.AssignmentTime)
            .Select(a => (int?)a.ElevatorCallId)
            .FirstOrDefault();


        await _hubContext.Clients.All.SendAsync("ElevatorUpdated", new ElevatorStatusDto
        {

            ElevatorId = elevator.Id,
            CurrentFloor = elevator.CurrentFloor,
            Direction = elevator.Direction.ToString(),
            Status = elevator.Status.ToString(),
            DoorStatus = elevator.DoorStatus.ToString(),
            LastCallId = lastCallId
        });
    }


    private void OpenAndChooseFloor(Elevator elevator)
    {
        elevator.Status = ElevatorStatus.OpeningDoors;
        elevator.DoorStatus = DoorStatus.Open;
        Thread.Sleep(3000);

        elevator.Status = ElevatorStatus.WaitingForDestination;
        Console.WriteLine($"[SERVER] Elevator {elevator.Id} status → WaitingForDestination");



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
