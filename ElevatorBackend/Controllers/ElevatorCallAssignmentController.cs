
using ElevatorBackend.Models;
using ElevatorBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace ElevatorBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElevatorCallAssignmentController : ControllerBase
    {
        private readonly IElevatorCallAssignmentService _assignmentService;

        public ElevatorCallAssignmentController(IElevatorCallAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        [HttpPost]
        public async Task<IActionResult> AssignElevatorToCall([FromBody] ElevatorCallAssignment assignment)
        {
            await _assignmentService.AssignElevatorToCallAsync(assignment);
            return Ok("Elevator assigned to call.");
        }

        [HttpGet("call/{callId}")]
        public async Task<IActionResult> GetAssignmentsByCall(int callId)
        {
            var assignments = await _assignmentService.GetAssignmentsByCallIdAsync(callId);
            return Ok(assignments);
        }

        [HttpGet("elevator/{elevatorId}")]
        public async Task<IActionResult> GetAssignmentsByElevator(int elevatorId)
        {
            var assignments = await _assignmentService.GetAssignmentsByElevatorIdAsync(elevatorId);
            return Ok(assignments);
        }
    }
}
