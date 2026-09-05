// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Application.Common.Exceptions;

/// <summary>
/// A deliberate HTTP error for a legacy endpoint that is outside the result migration.
/// </summary>
public sealed class HttpContractException(int statusCode, string title, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}
