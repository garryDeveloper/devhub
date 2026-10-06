namespace DevHub.Application.Projects.Contracts;

// { "name":"DevHub API", "key":"DEV", "description":"…", "color":"#4F46E5", "icon":"rocket" }
public sealed record ProjectDto
(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    string? Color,
    string? Icon
);
