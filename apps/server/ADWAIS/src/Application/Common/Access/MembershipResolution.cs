// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Application.Common.Access;

/// <summary>
/// The complete set of contexts a user may operate in, resolved from their
/// membership rows.
/// </summary>
public sealed record MembershipResolution(bool IsPlatformAdmin, IReadOnlyCollection<AllowedScope> OrgScopes);
