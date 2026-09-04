// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Security.Claims;
using Adwais.Api.DTOs.Users;
using Adwais.Api.Extensions;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Api.Controllers.Authentication;

/// <summary>
/// Manages system users and their database-assigned roles.
/// </summary>
[ApiController]
[Route("api/users")]
public class UserController(IUserService userService, ICurrentAccess currentAccess, IApplicationDbContext dbContext) : ControllerBase
{
    private readonly IUserService _userService = userService;
    private readonly ICurrentAccess _currentAccess = currentAccess;
    private readonly IApplicationDbContext _dbContext = dbContext;

    /// <summary>
    /// Resolves the authenticated OIDC subject or kiosk claims to the current application user.
    /// </summary>
    /// <remarks>
    /// OIDC users are matched by the standard <c>sub</c> claim. Kiosk tokens use their embedded
    /// role claims and do not create a database user record.
    /// </remarks>
    [HttpGet("me")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponseDto>> GetMe(CancellationToken ct)
    {
        var subjectId = User.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(subjectId))
        {
            var user = await _userService.GetUserByExternalSubjectIdAsync(subjectId, ct);
            if (user != null)
            {
                var effectiveRole = _currentAccess.Scope?.Roles.FirstOrDefault() ?? UserRole.Employee;
                var memberships = await _dbContext.UserAccesses
                    .AsNoTracking()
                    .Where(access => access.UserId == user.Id)
                    .ToListAsync(ct);
                var isPlatformAdmin = AccessScopeResolver.ResolveAllowed(memberships).IsPlatformAdmin;
                return Ok(await MapMeAsync(user.Id, user.Name, user.Email, effectiveRole, isPlatformAdmin, ct));
            }
        }

        var role = User.FindFirst("role")?.Value ?? User.FindFirst(global::System.Security.Claims.ClaimTypes.Role)?.Value;
        var kioskRole = role switch
        {
            "Admin"    => (UserRole?)UserRole.Admin,
            "Employee" => UserRole.Employee,
            "Viewer"   => UserRole.Viewer,
            "PlatformAdmin" => UserRole.PlatformAdmin,
            _          => null
        };

        if (!kioskRole.HasValue) return Unauthorized("User context is invalid or not registered.");

        var name = User.Identity?.Name ?? User.FindFirst("name")?.Value ?? "Kiosk Device";
        return Ok(await MapMeAsync(Guid.Empty, name, null, kioskRole.Value, false, ct));
    }

    /// <summary>
    /// Builds the profile payload with the effective scope attached. The scope
    /// fields are the single source the frontend uses for role and visibility.
    /// The platform flag comes from the user's memberships, not the per-request
    /// scope, so wearing an organization never strips platform status.
    /// </summary>
    private async Task<UserResponseDto> MapMeAsync(Guid id, string name, string? email, UserRole role, bool isPlatformAdmin, CancellationToken ct)
    {
        var scope = _currentAccess.Scope;
        string? organizationName = null;
        if (scope?.OrganizationId is { } orgId)
        {
            organizationName = await _dbContext.Organizations
                .AsNoTracking()
                .Where(org => org.Id == orgId)
                .Select(org => org.Name)
                .SingleOrDefaultAsync(ct);
        }

        return new UserResponseDto(
            id,
            name,
            email,
            role,
            scope?.OrganizationId,
            organizationName,
            scope?.TenantId,
            isPlatformAdmin);
    }

    [HttpGet]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers(CancellationToken ct)
    {
        var users = await _userService.GetUsersAsync(ct);
        var summaries = await ResolveMembershipSummaryByUserIdAsync(users.Select(u => u.Id).ToArray(), ct);
        var response = users.Select(u => new UserResponseDto(
            u.Id,
            u.Name,
            u.Email,
            summaries.TryGetValue(u.Id, out var summary) ? summary.Role : null,
            IsPlatformAdmin: summaries.TryGetValue(u.Id, out var s) && s.IsPlatformAdmin));
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<UserResponseDto>> GetUser(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetUserByIdAsync(id, ct);
        if (user == null)
        {
            return NotFound();
        }

        var (role, isPlatformAdmin) = await ResolveMembershipSummaryAsync(user.Id, ct);
        return Ok(new UserResponseDto(user.Id, user.Name, user.Email, role, IsPlatformAdmin: isPlatformAdmin));
    }

    /// <summary>
    /// Manually creates a new user record.
    /// Used for pre-provisioning users before OIDC sign-in.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserRequestDto request, CancellationToken ct)
    {
        var user = await _userService.CreateUserAsync(request.Email, request.Role, request.OrganizationId, ct);
        if (user.IsFailed) return user.ToProblem(HttpContext);

        var (role, isPlatformAdmin) = await ResolveMembershipSummaryAsync(user.Value.Id, ct);
        return CreatedAtAction(nameof(GetUser), new { id = user.Value.Id },
            new UserResponseDto(user.Value.Id, user.Value.Name, user.Value.Email, role, IsPlatformAdmin: isPlatformAdmin));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(Guid id, [FromBody] UpdateUserRequestDto request, CancellationToken ct)
    {
        var user = await _userService.UpdateUserAsync(id, request.Name, request.Role, ct);
        if (user.IsFailed) return user.ToProblem(HttpContext);

        var (role, isPlatformAdmin) = await ResolveMembershipSummaryAsync(user.Value.Id, ct);
        return Ok(new UserResponseDto(user.Value.Id, user.Value.Name, user.Value.Email, role, IsPlatformAdmin: isPlatformAdmin));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var success = await _userService.DeleteUserAsync(id, ct);
        if (success.IsFailed) return success.ToProblem(HttpContext);

        return NoContent();
    }

    /// <summary>
    /// Lists the membership rows of a user. Reach follows the service rules:
    /// organization admins see only their own organization's rows.
    /// </summary>
    [HttpGet("{id:guid}/memberships")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<IEnumerable<UserMembershipResponseDto>>> GetUserMemberships(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetUserByIdAsync(id, ct);
        if (user == null)
        {
            return NotFound();
        }

        var memberships = await _userService.GetUserMembershipsAsync(id, ct);
        return Ok(memberships.Select(MapMembership));
    }

    /// <summary>
    /// Adds a membership row for a user. Reach follows the service rules:
    /// organization admins may only add inside their own organization.
    /// </summary>
    [HttpPost("{id:guid}/memberships")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserMembershipResponseDto>> AddUserMembership(
        Guid id,
        [FromBody] AddUserMembershipRequestDto request,
        CancellationToken ct)
    {
        var user = await _userService.GetUserByIdAsync(id, ct);
        if (user == null)
        {
            return NotFound();
        }

        var membership = await _userService.AddUserMembershipAsync(id, request.OrganizationId, request.Role, ct);
        if (membership.IsFailed) return membership.ToProblem(HttpContext);

        return CreatedAtAction(nameof(GetUserMemberships), new { id }, MapMembership(membership.Value));
    }

    /// <summary>
    /// Removes a membership row from a user. The caller cannot remove their
    /// own platform-admin membership.
    /// </summary>
    [HttpDelete("{id:guid}/memberships/{membershipId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteUserMembership(Guid id, Guid membershipId, CancellationToken ct)
    {
        var user = await _userService.GetUserByIdAsync(id, ct);
        if (user == null)
        {
            return NotFound();
        }

        var callerUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var caller)
            ? caller
            : Guid.Empty;
        var removed = await _userService.RemoveUserMembershipAsync(id, membershipId, callerUserId, ct);
        if (removed.IsFailed) return removed.ToProblem(HttpContext);

        return NoContent();
    }

    private static UserMembershipResponseDto MapMembership(UserAccess membership)
        => new(
            membership.Id,
            membership.UserId,
            membership.OrganizationId,
            membership.Organization?.Name,
            membership.Role);

    /// <summary>
    /// Resolves the role and platform flag for one user. The role is the
    /// caller's-org role when the caller has an organization scope; in
    /// platform scope it is the dominant role across all memberships.
    /// The platform flag reflects membership, not the effective scope.
    /// </summary>
    private async Task<(UserRole? Role, bool IsPlatformAdmin)> ResolveMembershipSummaryAsync(Guid userId, CancellationToken ct)
    {
        var orgId = _currentAccess.Scope?.OrganizationId;
        var memberships = await _dbContext.UserAccesses
            .AsNoTracking()
            .Where(access => access.UserId == userId)
            .ToListAsync(ct);

        var role = orgId is null
            ? UserRoleResolver.Dominant(memberships)
            : memberships
                .Where(access => access.OrganizationId == orgId)
                .Select(access => (UserRole?)access.Role)
                .SingleOrDefault();

        return (role, UserRoleResolver.IsPlatformAdmin(memberships));
    }

    private async Task<Dictionary<Guid, (UserRole? Role, bool IsPlatformAdmin)>> ResolveMembershipSummaryByUserIdAsync(Guid[] userIds, CancellationToken ct)
    {
        var orgId = _currentAccess.Scope?.OrganizationId;
        var rows = await _dbContext.UserAccesses
            .AsNoTracking()
            .Where(access => userIds.Contains(access.UserId))
            .ToListAsync(ct);

        return rows
            .GroupBy(entry => entry.UserId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var memberships = group.ToList();
                    var role = orgId is null
                        ? UserRoleResolver.Dominant(memberships)
                        : memberships
                            .Where(access => access.OrganizationId == orgId)
                            .Select(access => (UserRole?)access.Role)
                            .SingleOrDefault();
                    return (role, UserRoleResolver.IsPlatformAdmin(memberships));
                });
    }
}
