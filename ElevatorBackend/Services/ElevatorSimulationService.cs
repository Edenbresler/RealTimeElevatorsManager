using ElevatorBackend.Data;
using ElevatorBackend.Hubs;
using ElevatorBackend.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ElevatorBackend.Models;

namespace ElevatorBackend.Services
{
    public class ElevatorSimulationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<ElevatorHub> _hubContext;

        public ElevatorSimulationService(IServiceScopeFactory scopeFactory, IHubContext<ElevatorHub> hubContext)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var elevatorService = scope.ServiceProvider.GetRequiredService<ElevatorService>();

                // 🟢 שלב 1: טיפול בקריאות שלא טופלו
                var unhandledCalls = await dbContext.ElevatorCalls
                    .Where(c => !c.IsHandled)
                    .ToListAsync();

                foreach (var call in unhandledCalls)
                {
                    var elevators = await dbContext.Elevators
                        .Include(e => e.AllRequests)
                        .Where(e => e.BuildingId == call.BuildingId)
                        .ToListAsync();

                    Elevator? selected = null;

                    // בדיקה אם יש מעלית מתאימה עכשיו
                    var idle = elevators
                        .Where(e => e.Status == ElevatorStatus.Idle)
                        .OrderBy(e => Math.Abs(e.CurrentFloor - call.RequestedFloor))
                        .FirstOrDefault();

                    if (idle != null)
                    {
                        selected = idle;

                        var direction = call.RequestedFloor > selected.CurrentFloor ? Direction.Up :
                                        call.RequestedFloor < selected.CurrentFloor ? Direction.Down : Direction.None;

                        selected.AddRequest(new ElevatorRequest
                        {
                            ElevatorId = selected.Id,
                            Floor = call.RequestedFloor,
                            Direction = direction,
                            Type = RequestType.Regular
                        });

                        // עדכון סטטוס
                        selected.Status = direction == Direction.Up ? ElevatorStatus.MovingUp :
                                          direction == Direction.Down ? ElevatorStatus.MovingDown :
                                          ElevatorStatus.Idle;

                        selected.Direction = direction;

                        call.IsHandled = true;
                        Console.WriteLine($"[Simulation] Assigned unhandled call {call.Id} to elevator {selected.Id}");
                    }
                }

                await dbContext.SaveChangesAsync();

                // 🟢 שלב 2: הפעלת מעליות
                foreach (var elevator in elevatorService.GetElevatorsWithRequests())

                {

                    Console.WriteLine($"[Simulation] Elevator {elevator.Id} has {elevator.AllRequests.Count} total requests");

                    Console.WriteLine($"[Simulation] Requests count for elevator {elevator.Id}: {elevator.AllRequests.Count}");
                    Console.WriteLine($"[Sim] Elevator {elevator.Id} → Status: {elevator.Status}, Requests: {elevator.Requests.Count}, Destinations: {elevator.DestinationRequests.Count}");


                    foreach (var req in elevator.AllRequests)
                    {
                        Console.WriteLine($"→ Request: Floor={req.Floor}, Type={req.Type}, Direction={req.Direction}");
                    }

                    _ = Task.Run(() => elevatorService.MoveToFloor(elevator.Id));

                    await _hubContext.Clients.All.SendAsync("ReceiveElevatorUpdate", new
                    {
                        ElevatorId = elevator.Id,
                        CurrentFloor = elevator.CurrentFloor,
                        Direction = elevator.Direction.ToString(),
                        Status = elevator.Status.ToString(),
                        DoorStatus = elevator.DoorStatus.ToString()
                    });
                }

                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }
    }
}
