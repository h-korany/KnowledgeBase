using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using FAQ.Infrastucture.Data;
using FAQ.Presentation.Models;

namespace FAQ.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private SignInManager<IdentityUser> _signInManager;
        private UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationSettings _applicationSettings;
        ApplicationIdentityDbContext ApplicationIdentityDbContext;

        public UserController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<ApplicationSettings> applicationSettings,
            ApplicationIdentityDbContext applicationIdentityDbContext)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _applicationSettings = applicationSettings.Value;
            ApplicationIdentityDbContext = applicationIdentityDbContext;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Find user by email (case insensitive)
                var user = await _userManager.FindByEmailAsync(model.Email.ToUpperInvariant());

                if (user == null)
                {
                    return BadRequest(new { message = "User not found." });
                }

                // Verify password
                var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
                if (!passwordValid)
                {
                    return BadRequest(new { message = "Invalid password." });
                }

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);

                // Generate token
                var claims = new List<Claim>
                {
                    new Claim("UserID", user.Id),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                // Add role claims
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddDays(1),
                    Issuer = _applicationSettings.JWT_Issuer,
                    Audience = _applicationSettings.JWT_Audience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_applicationSettings.JWT_Secret)),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var securityToken = tokenHandler.CreateToken(tokenDescriptor);
                var token = tokenHandler.WriteToken(securityToken);

                return Ok(new
                {
                    token = token,
                    userId = user.Id,
                    userName = user.UserName,
                    email = user.Email,
                    roles = roles
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, new { message = "An error occurred during login." });
            }
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate role
                if (model.Role != "employee" && model.Role != "manager")
                {
                    ModelState.AddModelError(nameof(model.Role), "Role must be either 'employee' or 'manager'");
                    return BadRequest(ModelState);
                }

                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Ensure the role exists
                    if (!await _roleManager.RoleExistsAsync(model.Role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(model.Role));
                    }

                    // Assign role to user
                    var roleResult = await _userManager.AddToRoleAsync(user, model.Role);

                    if (!roleResult.Succeeded)
                    {
                        // If role assignment fails, delete the user and return error
                        await _userManager.DeleteAsync(user);
                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return BadRequest(ModelState);
                    }

                    return Ok(new
                    {
                        message = "User registered successfully",
                        userId = user.Id,
                        role = model.Role
                    });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, new { message = "An error occurred during registration." });
            }
        }

        // Optional: Endpoint to get all available roles
        [HttpGet("roles")]
        public IActionResult GetAvailableRoles()
        {
            var roles = new List<string> { "employee", "manager" };
            return Ok(roles);
        }
    }
}