// ElevatorCallController.cs
using ElevatorBackend.Data;
using ElevatorBackend.Dtos;
using ElevatorBackend.Models;
using ElevatorBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElevatorBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElevatorCallController : ControllerBase
    {
        private readonly IElevatorCallService _elevatorCallService;
        private readonly AppDbContext _context;

        public ElevatorCallController(AppDbContext context, IElevatorCallService elevatorCallService)
        {
            _context = context;
            _elevatorCallService = elevatorCallService;
        }

        // נוסע מחוץ למעלית מזמין מעלית
        [HttpPost]
        public async Task<IActionResult> CreateCall([FromBody] ElevatorCallCreateDto dto)
        {
            var call = new ElevatorCall
            {
                BuildingId = dto.BuildingId,
                RequestedFloor = dto.RequestedFloor,
                DestinationFloor = dto.DestinationFloor,
                CallTime = DateTime.UtcNow,
                IsHandled = false
            };

            await _elevatorCallService.CreateCallAsync(call);
            return Ok("Call created");
        }

        // נוסע בתוך המעלית בוחר יעד
        [HttpPut("{id}/selectDestination")]
        public async Task<IActionResult> SelectDestination(int id, [FromBody] int destinationFloor)
        {
            Console.WriteLine($"[getServer] Received destinationFloor: {destinationFloor}");

            var result = await _elevatorCallService.UpdateDestinationFloorAsync(id, destinationFloor);
            if (!result)
                return NotFound();

            // לאחר עדכון יעד, אפשר לעדכן את סטטוס המעלית והכיוון שלה כאן אם רוצים
            var call = await _context.ElevatorCalls.FindAsync(id);
            if (call != null)
            {
                var elevatorAssignment = await _context.ElevatorCallAssignments
                    .FirstOrDefaultAsync(a => a.ElevatorCallId == id);

                if (elevatorAssignment != null)
                {
                    var elevator = await _context.Elevators.FindAsync(elevatorAssignment.ElevatorId);
                    if (elevator != null)
                    {
                        var direction = destinationFloor > elevator.CurrentFloor ? Direction.Up :
                                        destinationFloor < elevator.CurrentFloor ? Direction.Down : Direction.None;

                        elevator.Direction = direction;
                        elevator.Status = direction == Direction.Up ? ElevatorStatus.MovingUp :
                                           direction == Direction.Down ? ElevatorStatus.MovingDown : ElevatorStatus.Idle;

                        elevator.AllRequests.Add(new ElevatorRequest
                        {
                            ElevatorId = elevator.Id,
                            Floor = destinationFloor,
                            Direction = direction,
                            Type = RequestType.Destination
                        });

                        // נעדכן סטטוס ודלתות
                        elevator.DoorStatus = DoorStatus.Closed;
                        _context.Elevators.Update(elevator);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return Ok("Destination updated");
        }

        // המערכת מסמנת קריאה כמטופלת
        [HttpPut("{id}/markHandled")]
        public async Task<IActionResult> MarkHandled(int id)
        {
            var result = await _elevatorCallService.MarkCallAsHandledAsync(id);
            if (!result)
                return NotFound();

            return Ok("Call marked as handled");
        }

        // כל הקריאות - שימושי לדיבוג או ניהול
        [HttpGet]
        public async Task<IActionResult> GetAllCalls()
        {
            var calls = await _context.ElevatorCalls.ToListAsync();
            return Ok(calls);
        }
    }
}
