using ElevatorBackend.Data;
using ElevatorBackend.Models;
using Microsoft.EntityFrameworkCore;


namespace ElevatorBackend.Services
{
    public class ElevatorCallService : IElevatorCallService
    {
        private readonly AppDbContext _context;

        public ElevatorCallService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateCallAsync(ElevatorCall call)
        {
            call.CallTime = DateTime.UtcNow;
            _context.ElevatorCalls.Add(call);
            await _context.SaveChangesAsync();

            // Assign elevator immediately after saving the call
            var assignmentService = new ElevatorCallAssignmentService(_context);
            var assignment = new ElevatorCallAssignment
            {
                ElevatorCallId = call.Id,
                AssignmentTime = DateTime.UtcNow 
            };

            await assignmentService.AssignElevatorToCallAsync(assignment);
        }



        public async Task<bool> UpdateDestinationFloorAsync(int callId, int destinationFloor)
        {
            var call = await _context.ElevatorCalls.FindAsync(callId);
            if (call == null)
                return false;

            call.DestinationFloor = destinationFloor;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkCallAsHandledAsync(int callId)
        {
            var call = await _context.ElevatorCalls.FindAsync(callId);
            if (call == null)
                return false;

            call.IsHandled = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}