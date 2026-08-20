// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Users;
using Adwais.Application.Interfaces;
using Adwais.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Authentication;

/// <summary>
/// Manages system users and their database-assigned roles.
/// </summary>
[ApiController]
[Route("api/users")]
public class UserController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    /// <summary>
    /// Resolves the authenticated OIDC subject or kiosk claims to the current application user.
    /// </summary>
    /// <remarks>
    /// OIDC users are matched by the standard <c>sub</c> claim. Kiosk tokens use their embedded
    /// role claims and do not create a database user record.
    /// </remarks>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The authenticated application user, or the kiosk identity derived from the token.</returns>
    [HttpGet("me")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponseDto>> GetMe(CancellationToken ct)
    {
        Console.WriteLine("");
        var subjectId = User.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(subjectId))
        {
            var user = await _userService.GetUserByExternalSubjectIdAsync(subjectId, ct);
            if (user != null)
            {
                return Ok(new UserResponseDto(user.Id, user.Name, user.Email, user.Role));
            }
        }
        
        var role = User.FindFirst("role")?.Value ?? User.FindFirst(global::System.Security.Claims.ClaimTypes.Role)?.Value;
        var kioskRole = role switch
        {
            "Admin"    => (UserRole?)UserRole.Admin,
            "Employee" => UserRole.Employee,
            "Viewer"   => UserRole.Viewer,
            _          => null
        };

        if (!kioskRole.HasValue) return Unauthorized("User context is invalid or not registered.");
        
        var name = User.Identity?.Name ?? User.FindFirst("name")?.Value ?? "Kiosk Device";
        return Ok(new UserResponseDto(Guid.Empty, name, null, kioskRole.Value));

    }

    /// <summary>
    /// Lists all application users.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The users stored in the application database.</returns>
    [HttpGet]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers(CancellationToken ct)
    {
        var users = await _userService.GetUsersAsync(ct);
        var response = users.Select(u => new UserResponseDto(u.Id, u.Name, u.Email, u.Role));
        return Ok(response);
    }

    /// <summary>
    /// Retrieves an application user by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The requested user, or <see cref="NotFoundResult"/> when no user matches the ID.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<UserResponseDto>> GetUser(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetUserByIdAsync(id, ct);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserResponseDto(user.Id, user.Name, user.Email, user.Role));
    }

    /// <summary>
    /// Manually creates a new user record.
    /// Used for pre-provisioning users before OIDC sign-in.
    /// </summary>
    /// <param name="request">The email address and role for the new user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The newly created user.</returns>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserRequestDto request, CancellationToken ct)
    {
        var user = await _userService.CreateUserAsync(request.Email, request.Role, ct);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id },
            new UserResponseDto(user.Id, user.Name, user.Email, user.Role));
    }

    /// <summary>
    /// Updates the name or role of an existing user.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="request">The fields to update. Omitted fields keep their current values.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated user, or <see cref="NotFoundResult"/> when no user matches the ID.</returns>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(Guid id, [FromBody] UpdateUserRequestDto request, CancellationToken ct)
    {
        var user = await _userService.UpdateUserAsync(id, request.Name, request.Role, ct);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserResponseDto(user.Id, user.Name, user.Email, user.Role));
    }

    /// <summary>
    /// Deletes an application user.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>No content when the user is deleted, or <see cref="NotFoundResult"/> when no user matches the ID.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var success = await _userService.DeleteUserAsync(id, ct);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
