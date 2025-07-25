using ElevatorBackend.Data;
using ElevatorBackend.Models;
using ElevatorBackend.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;


public class ElevatorCallAssignmentService : IElevatorCallAssignmentService
{
    private readonly AppDbContext _context;

    public ElevatorCallAssignmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task AssignElevatorToCallAsync(ElevatorCallAssignment assignment)
    {
        var call = await _context.ElevatorCalls.FindAsync(assignment.ElevatorCallId);
        if (call == null)
            throw new Exception("Call not found");

        var elevators = await _context.Elevators
            .Include(e => e.AllRequests)
            .Where(e => e.BuildingId == call.BuildingId)
            .ToListAsync();

        if (!elevators.Any())
            throw new Exception("No elevators available in building");

        foreach (var e in elevators)
            Console.WriteLine($"Elevator {e.Id} - Status: {e.Status}, Direction: {e.Direction}, Floor: {e.CurrentFloor}");

        Elevator? selectedElevator = null;

        // 1. Elevator on the way
        var passingElevators = elevators.Where(e =>
            (e.Status == ElevatorStatus.MovingUp && call.RequestedFloor > e.CurrentFloor && e.Direction == Direction.Up) ||
            (e.Status == ElevatorStatus.MovingDown && call.RequestedFloor < e.CurrentFloor && e.Direction == Direction.Down)
        ).ToList();

        if (passingElevators.Any())
        {
            selectedElevator = passingElevators
                .OrderBy(e => Math.Abs(e.CurrentFloor - call.RequestedFloor))
                .First();
        }
        else
        {
            // 2. Idle elevator
            var idleElevators = elevators
                .Where(e => e.Status == ElevatorStatus.Idle)
                .OrderBy(e => Math.Abs(e.CurrentFloor - call.RequestedFloor))
                .ToList();

            if (idleElevators.Any())
            {
                selectedElevator = idleElevators.First();
            }
            else
            {
                // 3. Any elevator going in general direction
                var generalDirection = call.RequestedFloor > elevators.First().CurrentFloor ? Direction.Up : Direction.Down;

                var movingElevators = elevators
                    .Where(e => e.Direction == generalDirection)
                    .OrderBy(e => Math.Abs(e.CurrentFloor - call.RequestedFloor))
                    .ToList();

                if (movingElevators.Any())
                    selectedElevator = movingElevators.First();
            }
        }

        if (selectedElevator == null)
        {
            selectedElevator = elevators
                .OrderBy(e => Math.Abs(e.CurrentFloor - call.RequestedFloor))
                .FirstOrDefault();
        }

        // בדיקה האם אפשר לטפל מיידית בקריאה
        bool canHandleNow =
            (selectedElevator.Status == ElevatorStatus.MovingUp && call.RequestedFloor > selectedElevator.CurrentFloor && selectedElevator.Direction == Direction.Up) ||
            (selectedElevator.Status == ElevatorStatus.MovingDown && call.RequestedFloor < selectedElevator.CurrentFloor && selectedElevator.Direction == Direction.Down) ||
            selectedElevator.Status == ElevatorStatus.Idle;

        if (canHandleNow)
        {
            var direction = call.RequestedFloor > selectedElevator.CurrentFloor ? Direction.Up :
                            call.RequestedFloor < selectedElevator.CurrentFloor ? Direction.Down : Direction.None;

            selectedElevator.AddRequest(new ElevatorRequest
            {
                ElevatorId = selectedElevator.Id,
                Elevator = selectedElevator,
                Floor = call.RequestedFloor,
                Direction = direction,
                Type = RequestType.Regular
            });

            // עדכון סטטוס למעלית אם היא הייתה במצב Idle
            if (selectedElevator.Status == ElevatorStatus.Idle)
            {
                selectedElevator.Status = direction == Direction.Up ? ElevatorStatus.MovingUp :
                                           direction == Direction.Down ? ElevatorStatus.MovingDown :
                                           ElevatorStatus.Idle;

                selectedElevator.Direction = direction;
            }

            call.IsHandled = true; // הקריאה טופלה מיידית
        }

        // שמירת השיוך בין קריאה למעלית
        var newAssignment = new ElevatorCallAssignment
        {
            ElevatorCallId = call.Id,
            ElevatorId = selectedElevator.Id,
            AssignmentTime = DateTime.UtcNow
        };

        _context.ElevatorCallAssignments.Add(newAssignment);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ElevatorCallAssignment>> GetAssignmentsByElevatorIdAsync(int elevatorId)
    {
        return await _context.ElevatorCallAssignments
            .Where(a => a.ElevatorId == elevatorId)
            .ToListAsync();
    }

    public async Task<List<ElevatorCallAssignment>> GetAssignmentsByCallIdAsync(int callId)
    {
        return await _context.ElevatorCallAssignments
            .Where(a => a.ElevatorCallId == callId)
            .ToListAsync();
    }
}
