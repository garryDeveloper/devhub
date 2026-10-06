namespace DevHub.Application.Projects.Create;

// { "name":"DevHub API", "key":"DEV", "description":"…", "color":"#4F46E5", "icon":"rocket" }
public sealed record CreateProjectCommand(Guid WorkspaceId, string Name, string? Description, string Key, string? Color, string? Icon);
