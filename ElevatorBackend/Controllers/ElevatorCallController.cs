
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



        [HttpPut("{id}/selectDestination")]
        public async Task<IActionResult> SelectDestination(int id, [FromBody] int destinationFloor)
        {
            var result = await _elevatorCallService.UpdateDestinationFloorAsync(id, destinationFloor);
            if (!result)
                return NotFound();

            return Ok("Destination updated");
        }

        [HttpPut("{id}/markHandled")]
        public async Task<IActionResult> MarkHandled(int id)
        {
            var result = await _elevatorCallService.MarkCallAsHandledAsync(id);
            if (!result)
                return NotFound();

            return Ok("Call marked as handled");
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCalls()
        {
            var calls = await _context.ElevatorCalls.ToListAsync();
            return Ok(calls);
        }

    }
}