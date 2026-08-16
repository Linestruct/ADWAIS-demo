// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Application.Common.Access;

/// <summary>
/// The scope a principal may see. Null means no access at all.
/// </summary>
public interface ICurrentAccess
{
    AccessScope? Scope { get; }
}
