using Amazon.S3;
using MatchBy.Models;
using MatchBy.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Controllers;
[ApiController]
[Route("api/profile-image")]

public class ProfileImageController(IS3Service s3, UserManager<ApplicationUser> userManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        ApplicationUser? user = await userManager.Users
            .Where(u => u.Id == userManager.GetUserId(User))
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound("User not found.");
        }

        // Sem imagem → devolve avatar default
        if (user.ProfileImage is null)
        {
            return Redirect("/images/user-avatar.png");
        }

        if (user.ProfileImage.ExpireDateTimeUtc > DateTime.UtcNow)
        {
            return Redirect(user.ProfileImage.Url);
        }
        
        // Gera URL da imagem no S3
        string? url = await s3.GetPresignedUrlAsync(
            $"users/{user.Id}/profile-pictures/{user.ProfileImage.Key}",
            HttpVerb.GET
        );

        if (url is null)
        {
            return NotFound("Could not generate presigned URL.");
        }

        user.ProfileImage = user.ProfileImage with { Url = url, ExpireDateTimeUtc = DateTime.UtcNow.AddMinutes(15) };
        
        await userManager.UpdateAsync(user);
        
        return Redirect(url);
    }
    
    [Authorize]
    [HttpPost("remove")]
    public async Task<IActionResult> RemoveImage([FromServices] IAntiforgery antiforgery)
    {
        await antiforgery.ValidateRequestAsync(HttpContext);
        ApplicationUser? user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }
        
        if (user.ProfileImage is not null)
        {
            string key = user.ProfileImage.Key;
            await s3.DeleteFileAsync($"users/{user.Id}/profile-pictures/{key}");
        }
        
        user.ProfileImage = null;
        await userManager.UpdateAsync(user);
        
        return Redirect("/Account/Manage");
    }
}
