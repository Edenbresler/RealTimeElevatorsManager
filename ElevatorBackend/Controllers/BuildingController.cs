using ElevatorBackend.Dtos;
using ElevatorBackend.Models;
using ElevatorBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace ElevatorBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BuildingController : ControllerBase
    {
        private readonly BuildingService _buildingService;

        public BuildingController(BuildingService buildingService)
        {
            _buildingService = buildingService;
        }

        [HttpGet]
        public ActionResult<List<Building>> GetAll()
        {
            return Ok(_buildingService.GetAll());
        }

        [HttpGet("{id}")]
        public ActionResult<Building> GetById(int id)
        {
            var building = _buildingService.GetById(id);
            if (building == null)
                return NotFound();

            return Ok(building);
        }


        [HttpPost]
        public ActionResult<Building> Create(BuildingCreateDto dto)
        {
            var building = new Building
            {
                Name = dto.Name,
                NumberOfFloors = dto.NumberOfFloors,
                UserId = dto.UserId 
            };

            _buildingService.Add(building);
            return CreatedAtAction(nameof(GetById), new { id = building.Id }, building);


        }

        [HttpGet("user/{userId}")]
        public ActionResult<List<BuildingDto>> GetByUserId(int userId)
        {
            var buildings = _buildingService.GetByUserId(userId)
                .Select(b => new BuildingDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    NumberOfFloors = b.NumberOfFloors
                }).ToList();
            
            if (buildings == null || buildings.Count == 0)
                return NotFound("No buildings found for this user.");

            return Ok(buildings);
        }



    }
}
