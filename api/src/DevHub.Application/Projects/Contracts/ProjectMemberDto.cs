using System;
using System.Collections.Generic;
using System.Text;
using DevHub.Application.Users.Contracts;

namespace DevHub.Application.Projects.Contracts;

public sealed record ProjectMemberDto(Guid Id, UserDto User, string Role, DateTimeOffset JoinedAt);
