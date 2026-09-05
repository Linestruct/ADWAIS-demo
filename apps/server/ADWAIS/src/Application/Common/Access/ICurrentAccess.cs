// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.Common.Access;

/// <summary>
/// The scope a principal may see. Null means no access at all.
/// </summary>
public interface ICurrentAccess
{
    AccessScope? Scope { get; }
}
