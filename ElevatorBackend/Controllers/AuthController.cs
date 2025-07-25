using ElevatorBackend.Models;
using ElevatorBackend.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ElevatorBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public AuthController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(User user)
        {
            var existingUser = await _userRepository.GetUserByEmailAsync(user.Email);
            if (existingUser != null)
                return BadRequest("User already exists");

            await _userRepository.AddUserAsync(user);
            return Ok("User registered successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(User user)
        {
            var dbUser = await _userRepository.GetUserByEmailAsync(user.Email);
            if (dbUser == null || dbUser.Password != user.Password)
                return Unauthorized("Invalid credentials");

            return Ok("Login successful");
        }
    }
}
