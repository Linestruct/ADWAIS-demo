// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.Common.Exceptions;

/// <summary>
/// A deliberate HTTP error for a legacy endpoint that is outside the result migration.
/// </summary>
public sealed class HttpContractException(int statusCode, string title, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}
