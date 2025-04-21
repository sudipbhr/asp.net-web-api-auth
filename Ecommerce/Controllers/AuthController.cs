using Ecommerce.DTOs.Auth;
using Ecommerce.Models;
using Ecommerce.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;


namespace Ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly TokenService _tokenService;
        private readonly IConfiguration _configuration;


        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            TokenService tokenService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid request"
                });
            }

            var userExists = await _userManager.FindByNameAsync(model.UserName);
            if (userExists != null)
            {
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "User already exists"
                });
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                MobileNumber = model.MobileNumber,
                Address = model.Address,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "User creation failed: " + string.Join(", ", result.Errors.Select(e => e.Description))
                });
            }

            // Ensure the "Customer" role exists, create it if it doesn't
            if (!await _roleManager.RoleExistsAsync("Customer"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Customer"));
            }

            // Add user to Customer role
            await _userManager.AddToRoleAsync(user, "Customer");

            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Message = "User created successfully"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid request"
                });
            }

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                return Unauthorized(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid username or password"
                });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid username or password"
                });
            }

            var token = await _tokenService.GenerateToken(user);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Token = token,
                UserName = user.UserName,
                Email = user.Email,
                Message = "Login successful",
                Roles = roles.ToList()
            });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDTO model)
        {
            //try
            //{
                if (string.IsNullOrEmpty(model.IdToken))
                {
                    return BadRequest(new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = "Google token is required"
                    });
                }

                // Validate Google token
                var payload = await GoogleJsonWebSignature.ValidateAsync(
                    model.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _configuration["Google:ClientId"] }
                    });
              


                // Check if user exists
                var user = await _userManager.FindByEmailAsync(payload.Email);

                if (user == null)
                {
                    // Create new user if doesn't exist
                    user = new ApplicationUser
                    {
                        UserName = payload.Email,
                        Email = payload.Email,
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString()
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        return BadRequest(new AuthResponseDto
                        {
                            IsSuccess = false,
                            Message = "User creation failed: " +
                                string.Join(", ", createResult.Errors.Select(e => e.Description))
                        });
                    }

                    // Ensure Customer role exists
                    if (!await _roleManager.RoleExistsAsync("Customer"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Customer"));
                    }

                    // Add to Customer role
                    var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
                    if (!roleResult.Succeeded)
                    {
                        return BadRequest(new AuthResponseDto
                        {
                            IsSuccess = false,
                            Message = "Failed to assign role: " +
                                string.Join(", ", roleResult.Errors.Select(e => e.Description))
                        });
                    }
                }

                // Generate JWT token
                var token = await _tokenService.GenerateToken(user);
                var roles = await _userManager.GetRolesAsync(user);

            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Token = token,
                UserName = user.UserName,
                Email = user.Email,
                Message = "Google login successful",
                Roles = roles.ToList()
            });
        }
    }
}

