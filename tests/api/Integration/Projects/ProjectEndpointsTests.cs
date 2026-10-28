using System.Net;
using System.Net.Http.Json;
using Coordina.Api.Tests.Support;

namespace Coordina.Api.Tests;

public sealed class ProjectEndpointsTests(ApiTestApplicationFactory factory)
  : ApiTest(factory)
{
  [Fact]
  public async Task CreateProject_WithinWorkspace_ReturnsActiveProject()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Product Team");

    var response = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects",
      owner.AccessToken,
      new
      {
        name = "Launch plan",
        description = "Coordinate launch work.",
        key = "app",
        icon = "🚀",
        color = "teal"
      });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var project = await response.Content.ReadFromJsonAsync<ProjectResponse>();

    Assert.NotNull(project);
    Assert.Equal("Launch plan", project.Name);
    Assert.Equal("Coordinate launch work.", project.Description);
    Assert.Equal("APP", project.Key);
    Assert.Equal("ACTIVE", project.Status);
    Assert.Equal(workspace.Id, project.WorkspaceId);
    Assert.Equal(owner.User.Id, project.ProjectOwnerId);
    Assert.Equal(project.CreatedAt, project.UpdatedAt);
    Assert.Null(project.ArchivedAt);
  }

  [Fact]
  public async Task ListProjects_FiltersByStatusInBackend()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Product Team");
    var active = await Api.CreateProjectAsync(owner.AccessToken, workspace.Id, "Active");
    var archived = await Api.CreateProjectAsync(owner.AccessToken, workspace.Id, "Archived");
    var completed = await Api.CreateProjectAsync(owner.AccessToken, workspace.Id, "Completed");
    await Api.UpdateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      archived.Id,
      new { status = "ARCHIVED" });
    await Api.UpdateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      completed.Id,
      new { status = "COMPLETED" });

    var defaultProjects = await Api.ListProjectsAsync(owner.AccessToken, workspace.Id);
    var withArchived = await Api.ListProjectsAsync(
      owner.AccessToken,
      workspace.Id,
      "?includeArchived=true");
    var withCompleted = await Api.ListProjectsAsync(
      owner.AccessToken,
      workspace.Id,
      "?includeCompleted=true");
    var withAll = await Api.ListProjectsAsync(
      owner.AccessToken,
      workspace.Id,
      "?includeArchived=true&includeCompleted=true");

    Assert.Contains(defaultProjects, project => project.Id == active.Id);
    Assert.DoesNotContain(defaultProjects, project => project.Id == archived.Id);
    Assert.DoesNotContain(defaultProjects, project => project.Id == completed.Id);

    Assert.Contains(withArchived, project => project.Id == archived.Id);
    Assert.DoesNotContain(withArchived, project => project.Id == completed.Id);

    Assert.Contains(withCompleted, project => project.Id == completed.Id);
    Assert.DoesNotContain(withCompleted, project => project.Id == archived.Id);

    Assert.Contains(withAll, project => project.Id == active.Id);
    Assert.Contains(withAll, project => project.Id == archived.Id);
    Assert.Contains(withAll, project => project.Id == completed.Id);
  }

  [Fact]
  public async Task DeleteProject_ArchivesByDefault_AndOwnerCanPermanentlyDelete()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Ops Team");
    var project = await Api.CreateProjectAsync(owner.AccessToken, workspace.Id, "Cleanup");

    var archiveResponse = await Api.SendAsync(
      HttpMethod.Delete,
      $"/workspaces/{workspace.Id}/projects/{project.Id}",
      owner.AccessToken);

    Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

    var archived = await Api.GetProjectAsync(owner.AccessToken, workspace.Id, project.Id);

    Assert.Equal("ARCHIVED", archived.Status);
    Assert.NotNull(archived.ArchivedAt);

    var defaultProjects = await Api.ListProjectsAsync(owner.AccessToken, workspace.Id);

    Assert.DoesNotContain(defaultProjects, item => item.Id == project.Id);

    var permanentDeleteResponse = await Api.SendAsync(
      HttpMethod.Delete,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/permanent",
      owner.AccessToken);

    Assert.Equal(HttpStatusCode.NoContent, permanentDeleteResponse.StatusCode);

    var getResponse = await Api.SendAsync(
      HttpMethod.Get,
      $"/workspaces/{workspace.Id}/projects/{project.Id}",
      owner.AccessToken);

    Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
  }

  [Fact]
  public async Task ProjectOwner_CanEditAndArchiveProject()
  {
    var owner = await Api.RegisterAsync();
    var member = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Launch Team");
    var invite = await Api.CreateInviteAsync(owner.AccessToken, workspace.Id);
    await Api.JoinWorkspaceAsync(member.AccessToken, invite.Code);
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Member owned",
      member.User.Id);

    var updateResponse = await Api.SendAsync(
      HttpMethod.Patch,
      $"/workspaces/{workspace.Id}/projects/{project.Id}",
      member.AccessToken,
      new { name = "Member updated", color = "pink" });

    Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

    var updated = await updateResponse.Content.ReadFromJsonAsync<ProjectResponse>();

    Assert.NotNull(updated);
    Assert.Equal("Member updated", updated.Name);
    Assert.Equal("pink", updated.Color);

    var archiveResponse = await Api.SendAsync(
      HttpMethod.Delete,
      $"/workspaces/{workspace.Id}/projects/{project.Id}",
      member.AccessToken);

    Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
  }

  [Fact]
  public async Task Member_WhoIsNotProjectOwner_CannotModifyProject()
  {
    var owner = await Api.RegisterAsync();
    var member = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Private Team");
    var invite = await Api.CreateInviteAsync(owner.AccessToken, workspace.Id);
    await Api.JoinWorkspaceAsync(member.AccessToken, invite.Code);
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Owner project");

    var updateResponse = await Api.SendAsync(
      HttpMethod.Patch,
      $"/workspaces/{workspace.Id}/projects/{project.Id}",
      member.AccessToken,
      new { name = "Nope" });
    var archiveResponse = await Api.SendAsync(
      HttpMethod.Delete,
      $"/workspaces/{workspace.Id}/projects/{project.Id}",
      member.AccessToken);
    var permanentDeleteResponse = await Api.SendAsync(
      HttpMethod.Delete,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/permanent",
      member.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, archiveResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, permanentDeleteResponse.StatusCode);
  }

  [Fact]
  public async Task CompletedProjects_AreReadOnly()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Readonly Team");
    var project = await Api.CreateProjectAsync(owner.AccessToken, workspace.Id, "Done");
    var completed = await Api.UpdateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      new { status = "COMPLETED" });

    Assert.Equal("COMPLETED", completed.Status);

    var updateResponse = await Api.SendAsync(
      HttpMethod.Patch,
      $"/workspaces/{workspace.Id}/projects/{project.Id}",
      owner.AccessToken,
      new { name = "Should not change" });
    var archiveResponse = await Api.SendAsync(
      HttpMethod.Delete,
      $"/workspaces/{workspace.Id}/projects/{project.Id}",
      owner.AccessToken);

    Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Conflict, archiveResponse.StatusCode);
  }

  [Fact]
  public async Task ProjectOwner_MustBeWorkspaceMember()
  {
    var owner = await Api.RegisterAsync();
    var outsider = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Owner Guard");

    var response = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects",
      owner.AccessToken,
      new
      {
        name = "Invalid owner",
        projectOwnerId = outsider.User.Id
      });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Projects_ForNonMember_ReturnNotFound()
  {
    var owner = await Api.RegisterAsync();
    var outsider = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Private");
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Private project");

    var listResponse = await Api.SendAsync(
      HttpMethod.Get,
      $"/workspaces/{workspace.Id}/projects",
      outsider.AccessToken);
    var getResponse = await Api.SendAsync(
      HttpMethod.Get,
      $"/workspaces/{workspace.Id}/projects/{project.Id}",
      outsider.AccessToken);

    Assert.Equal(HttpStatusCode.NotFound, listResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
  }

  [Fact]
  public async Task ProjectDetails_WithWrongWorkspaceScope_ReturnsNotFound()
  {
    var owner = await Api.RegisterAsync();
    var firstWorkspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Scoped");
    var secondWorkspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Other");
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      firstWorkspace.Id,
      "Scoped project");

    var response = await Api.SendAsync(
      HttpMethod.Get,
      $"/workspaces/{secondWorkspace.Id}/projects/{project.Id}",
      owner.AccessToken);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task Projects_WithoutToken_ReturnUnauthorized()
  {
    var response = await Api.Http.GetAsync(
      $"/workspaces/{Guid.NewGuid()}/projects");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }
}
