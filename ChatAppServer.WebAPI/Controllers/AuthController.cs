using ChatAppServer.WebAPI.Context;
using ChatAppServer.WebAPI.Dtos;
using ChatAppServer.WebAPI.Models;
using GenericFileService.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatAppServer.WebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public sealed class AuthController(ApplicationDbContext context) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Register([FromForm] RegisterDto request,CancellationToken cancellationToken)
        {
            bool isNameExsist = await context.Users.AnyAsync(p=> p.Name == request.Name,cancellationToken);

            if (isNameExsist)
            {
                return BadRequest(new { Message = "Bu kullanıcı adı daha önce kulanılmıştır" });
            }
            string avatar = FileService.FileSaveToServer(request.file, "wwwroot/avatar/");
            User user = new()
            {
                Name = request.Name,
                Avatar = avatar
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync(cancellationToken);

            return Ok(user);
        }
        [HttpGet]
        public async Task<IActionResult> Login(string name, CancellationToken cancellationToken)
        {
            User? user = await context.Users.FirstOrDefaultAsync(u => u.Name == name, cancellationToken);
            if (user is null) 
            {
                return BadRequest(new { Message = "Kullanıcı Bulunamadı" });
            }
            user.Status = "Online";
            await context.SaveChangesAsync(cancellationToken);
            return Ok(user);
        }
    }
}
