using System;
using System.Collections.Generic;
using System.Text;

namespace DevHub.Application.Projects.Members.Remove;

public record RemoveProjectMemberCommand(Guid ProjectId, Guid MemberId);

