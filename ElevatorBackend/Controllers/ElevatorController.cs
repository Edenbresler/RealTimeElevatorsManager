using ElevatorBackend.Dtos;
using ElevatorBackend.Models;
using ElevatorBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace ElevatorBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElevatorController : ControllerBase
    {
        private readonly ElevatorService _elevatorService;

        public ElevatorController(ElevatorService elevatorService)
        {
            _elevatorService = elevatorService;
        }

        [HttpGet]
        public ActionResult<List<ElevatorDto>> GetAll()
        {
            var elevators = _elevatorService.GetAll();
            var dtoList = elevators.Select(e => new ElevatorDto
            {
                Id = e.Id,
                BuildingId = e.BuildingId,
                CurrentFloor = e.CurrentFloor,
                Status = e.Status,
                Direction = e.Direction,
                DoorStatus = e.DoorStatus
            }).ToList();

            return Ok(dtoList);
        }

        [HttpGet("{id}")]
        public ActionResult<ElevatorDto> GetById(int id)
        {
            var elevator = _elevatorService.GetById(id);
            if (elevator == null)
                return NotFound();

            var dto = new ElevatorDto
            {
                Id = elevator.Id,
                BuildingId = elevator.BuildingId,
                CurrentFloor = elevator.CurrentFloor,
                Status = elevator.Status,
                Direction = elevator.Direction,
                DoorStatus = elevator.DoorStatus
            };

            return Ok(dto);
        }

        [HttpGet("by-building/{buildingId}")]
        public ActionResult<List<ElevatorDto>> GetByBuildingId(int buildingId)
        {
            var elevators = _elevatorService.GetAll()
                .Where(e => e.BuildingId == buildingId)
                .ToList();

            if (elevators.Count == 0)
                return NotFound($"No elevators found for building {buildingId}");

            var dtoList = elevators.Select(e => new ElevatorDto
            {
                Id = e.Id,
                BuildingId = e.BuildingId,
                CurrentFloor = e.CurrentFloor,
                Status = e.Status,
                Direction = e.Direction,
                DoorStatus = e.DoorStatus
            }).ToList();

            return Ok(dtoList);
        }

        [HttpPost]
        public ActionResult<ElevatorDto> CreateElevator(ElevatorCreateDto dto)
        {
            var newElevator = new Elevator
            {
                BuildingId = dto.BuildingId,
                CurrentFloor = 0,
                Status = ElevatorStatus.Idle,
                Direction = Direction.None,
                DoorStatus = DoorStatus.Closed,
            };

            _elevatorService.AddElevator(newElevator);

            var response = new ElevatorDto
            {
                Id = newElevator.Id,
                BuildingId = newElevator.BuildingId,
                CurrentFloor = newElevator.CurrentFloor,
                Status = newElevator.Status,
                Direction = newElevator.Direction,
                DoorStatus = newElevator.DoorStatus
            };

            return CreatedAtAction(nameof(GetAll), new { id = newElevator.Id }, response);
        }
    }
}
