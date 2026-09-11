// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.ComponentModel.DataAnnotations;
using System.Linq;
using Adwais.Api.DTOs.Users;
using Xunit;

namespace Adwais.Tests.DTOs;

public class AddUserMembershipRequestDtoTests
{
    [Fact]
    public void RoleRequiredMetadataIsAttachedToTheRecordParameter()
    {
        var constructor = typeof(AddUserMembershipRequestDto).GetConstructors().Single();
        var roleParameter = constructor.GetParameters().Single(parameter => parameter.Name == "Role");
        var roleProperty = typeof(AddUserMembershipRequestDto).GetProperty(nameof(AddUserMembershipRequestDto.Role));

        Assert.True(roleParameter.IsDefined(typeof(RequiredAttribute), inherit: false));
        Assert.NotNull(roleProperty);
        Assert.False(roleProperty!.IsDefined(typeof(RequiredAttribute), inherit: false));
    }
}
