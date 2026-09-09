// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.Common.Access;

/// <summary>
/// The complete set of contexts a user may operate in, resolved from their
/// membership rows.
/// </summary>
public sealed record MembershipResolution(bool IsPlatformAdmin, IReadOnlyCollection<AllowedScope> OrgScopes);
