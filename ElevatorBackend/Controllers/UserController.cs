using ElevatorBackend.DTOs;
using ElevatorBackend.Models;
using ElevatorBackend.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ElevatorBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto userDto)
        {
            var existingUser = await _userRepository.GetUserByEmailAsync(userDto.Email);
            if (existingUser != null)
                return BadRequest("User with this email already exists.");

            var user = new User
            {
                Email = userDto.Email,
                Password = userDto.Password
            };

            await _userRepository.AddUserAsync(user);
            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto loginDto)
        {
            var authenticatedUser = await _userRepository.LoginAsync(loginDto.Email, loginDto.Password);
            if (authenticatedUser == null)
                return Unauthorized("Invalid email or password.");

            return Ok(new
            {
                message = "Login successful",
                userId = authenticatedUser.Id,
                email = authenticatedUser.Email

            });
        }
    }
}
