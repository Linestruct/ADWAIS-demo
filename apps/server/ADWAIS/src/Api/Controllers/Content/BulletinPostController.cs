// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Security.Claims;
using Adwais.Api.DTOs.Intranet;
using Adwais.Api.Extensions;
using Adwais.Application.DTOs.Intranet;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Content;

[ApiController]
[Route("api/intranet/bulletin-posts")]
[Authorize(Policy = "KioskOrStaffAccess")]
public class BulletinPostController(IBulletinPostService postService) : ControllerBase
{
    private readonly IBulletinPostService _postService = postService;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BulletinPostResponseDto>> GetPost(Guid id, CancellationToken ct)
    {
        var post = await _postService.GetPostByIdAsync(id, ct);
        if (post == null) return NotFound();
        return Ok(ToResponse(post));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BulletinPostResponseDto>>> GetPosts(CancellationToken ct)
    {
        var posts = await _postService.GetPostsAsync(ct);
        return Ok(posts.Select(ToResponse).ToList());
    }

    [HttpPost]
    [Authorize(Policy = "StaffAccess")]
    [ProducesResponseType(typeof(BulletinPostResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulletinPostResponseDto>> CreatePost([FromBody] CreateBulletinPostDto dto, CancellationToken ct)
    {
        var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
        {
            return Unauthorized("User context is invalid.");
        }

        var post = await _postService.CreatePostAsync(userId, dto.Title, dto.Body, ct);
        if (post.IsFailed) return post.ToProblem(HttpContext);

        return CreatedAtAction(nameof(GetPost), new { id = post.Value.Id }, ToResponse(post.Value));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "StaffAccess")]
    [ProducesResponseType(typeof(BulletinPostResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BulletinPostResponseDto>> UpdatePost(
        Guid id,
        [FromBody] UpdateBulletinPostDto dto,
        CancellationToken ct)
    {
        var post = await _postService.GetPostByIdAsync(id, ct);
        if (post == null) return NotFound();

        var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("Admin");
        var isAuthor = Guid.TryParse(nameIdentifier, out var userId) && post.UserId == userId;
        if (!isAdmin && !isAuthor) return Forbid();

        var updated = await _postService.UpdatePostAsync(id, dto.Title, dto.Body, ct);
        return updated.IsFailed ? updated.ToProblem(HttpContext) : Ok(ToResponse(updated.Value));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "StaffAccess")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken ct)
    {
        var post = await _postService.GetPostByIdAsync(id, ct);
        if (post == null) return NotFound();

        var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("Admin");
        var isAuthor = Guid.TryParse(nameIdentifier, out var userId) && post.UserId == userId;
        if (!isAdmin && !isAuthor) return Forbid();

        var deleted = await _postService.DeletePostAsync(id, ct);
        return deleted.IsFailed ? deleted.ToProblem(HttpContext) : NoContent();
    }

    private static BulletinPostResponseDto ToResponse(BulletinPost post)
    {
        return new BulletinPostResponseDto(
            post.Id,
            post.Title,
            post.Body,
            post.CreatedAt,
            post.UpdatedAt,
            post.User == null ? null : new BulletinPostAuthorDto(post.User.Id, post.User.Name));
    }
}
