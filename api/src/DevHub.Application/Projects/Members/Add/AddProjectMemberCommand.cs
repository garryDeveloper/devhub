using System;
using System.Collections.Generic;
using System.Text;

namespace DevHub.Application.Projects.Members.Add;

public sealed record AddProjectMemberCommand(Guid ProjectId, Guid UserId);
