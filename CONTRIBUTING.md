# Contributing to Coordina

Thank you for taking the time to improve Coordina. The project is currently a pre-MVP foundation, so contributions should strengthen the platform shape instead of adding disconnected product fragments.

## Development Principles

Coordina favors:

| Principle | Expectation |
| --- | --- |
| Clear module boundaries | Keep backend changes inside the owning feature module unless shared infrastructure is truly required |
| Server-side authorization | Never rely on frontend state for access control |
| Small pull requests | Prefer focused changes with clear behavior and tests |
| Typed contracts | Keep API contracts explicit and frontend clients typed |
| Repeatable quality | The full quality gate should pass locally before review |
| Product restraint | Prefer foundations that make the first real MVP easier to build |

## Local Setup

Install dependencies:

```bash
pnpm --dir src/web install
dotnet restore Coordina.sln -m:1
dotnet tool restore
```

Create your environment file:

```bash
cp .env.example .env
```

Start local infrastructure:

```bash
make docker-up
```

Apply migrations:

```bash
make api-migrate
```

Run the app:

```bash
make dev
```

## Contribution Workflow

By participating you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).
Questions go to [Discussions](https://github.com/Neptune2k21/Coordina/discussions),
vulnerabilities to a private advisory (see [SECURITY.md](SECURITY.md)).

### 1. Start from an issue

Every pull request starts from an issue, except typo or one-line doc fixes.

1. Search the existing issues, then open one with the bug or feature template.
2. A maintainer triages it: `status: needs triage` becomes `status: accepted`,
   `status: needs info`, or a `resolution:` label.
3. Comment on an accepted issue to take it, and wait to be assigned before
   starting. This avoids two people building the same thing.

New to the project? Look for
[`good first issue`](https://github.com/Neptune2k21/Coordina/labels/good%20first%20issue)
and [`help wanted`](https://github.com/Neptune2k21/Coordina/labels/help%20wanted).

### 2. Work in your fork

Only maintainers create branches in this repository. Contributors work in a fork:

```bash
gh repo fork Neptune2k21/Coordina --clone
cd Coordina
git switch -c fix/invite-consumption-race
```

Name branches `<type>/<short-description>`, using the commit types below.
Keep your branch current by rebasing, not merging:

```bash
git fetch upstream
git rebase upstream/master
```

### 3. Open a pull request against `master`

`master` is the only long-lived branch. It is protected:

| Rule | Effect |
| --- | --- |
| Pull request required | Nobody pushes to `master` directly |
| 1 approval from a code owner | See `.github/CODEOWNERS` |
| New commits dismiss approvals | A reviewer approves what is actually merged |
| Conversations resolved | Every review thread is closed before merging |
| `Format, lint, test and build` passes | The full quality gate runs in CI |
| `Conventional title` passes | The PR title follows the commit style below |
| Branch up to date | The branch is rebased on the latest `master` |
| Squash merge only | One pull request becomes one commit on `master` |

Open a draft pull request early if you want feedback. Link the issue with
`Closes #123` in the description. For a first contribution, a maintainer has to
approve the CI run before it starts.

### 4. Labels

| Group | Labels | Set by |
| --- | --- | --- |
| Type | `type: bug`, `type: feature`, `type: docs`, `type: refactor`, `type: test`, `type: chore`, `type: security`, `type: dependencies` | Issue template or maintainer |
| Area | `area: api`, `area: web`, `area: graph-engine`, `area: infra` | Labeler workflow on pull requests, maintainer on issues |
| Status | `status: needs triage`, `status: needs info`, `status: accepted`, `status: blocked` | Maintainer |
| Priority | `priority: critical`, `priority: high`, `priority: low` | Maintainer |
| Resolution | `resolution: duplicate`, `resolution: invalid`, `resolution: wontfix` | Maintainer |
| Community | `good first issue`, `help wanted` | Maintainer |
| Release | `breaking change` | Maintainer |

Release notes are grouped by the `type:` label of each merged pull request.

## Backend Guidelines

The API follows a feature-module layout:

```text
src/api/modules/<feature>
├── Application
├── Contracts
├── Domain
├── Infrastructure
├── <Feature>Module.cs
└── <Feature>Endpoints.cs
```

Modules build on two foundation layers instead of re-implementing plumbing:
`src/api/core` (module contract, results, HTTP mapping, current user,
persistence) and `src/api/shared` (validation, text and enum helpers). See
`docs/architecture.mdx` for the full list.

When adding backend behavior:

1. Put business rules in `Application`.
2. Keep HTTP request and response shapes in `Contracts`.
3. Keep EF entities, their `IEntityTypeConfiguration`, and database queries in `Infrastructure`.
4. Return `Result`/`Result<T>` from services (`return Error.NotFound();`) instead of throwing for normal user-facing outcomes.
5. Build field errors with `ValidationErrors` (`Required`, `MaxLength`, `Code<TEnum>` for enum fields), normalize input with `TextNormalizer`, and format enums with `EnumCode`.
6. Map endpoint groups with `MapApiGroup`, which requires an authenticated user. Mark public endpoints with `AllowAnonymous()` and add them to `EndpointAuthorizationTests`.
7. In endpoints, take `CurrentUser` and map results with `ToOkResult`, `ToCreatedResult`, or `ToNoContentResult`.
8. Inject `TimeProvider` instead of reading `DateTimeOffset.UtcNow`.
9. Register a new module by implementing `IModule` and adding it to `AddModules` in `Program.cs`.
10. Add integration tests for endpoint behavior.
11. Add migrations when persistence changes.

### Automated rules

These conventions are tests, so `make api-test` reports a violation before a
reviewer has to:

| Test | Rule |
| --- | --- |
| `LayerDependencyTests` | `core` never references `shared` or modules; `shared` never references modules |
| `ModuleConventionTests` | Modules use `TimeProvider`, `CurrentUser`, core results, and touch `CoordinaDbContext` only in `Infrastructure` |
| `EndpointAuthorizationTests` | Every endpoint requires authorization or is explicitly anonymous, and anonymous routes are allow-listed |
| `ModuleRegistrationTests` | Every `IModule` implementation is registered in `Program.cs` |

If a rule blocks a legitimate change, change the rule in the same pull request
and explain why.

Authorization rules must be enforced by the API. Frontend checks may improve UX, but they are not security boundaries.

For now, avoid adding public API documentation sections to the README for every endpoint. Endpoint details should live in OpenAPI/Scalar and tests; the root README should explain the project, architecture, and current state.

## Frontend Guidelines

The web app is organized by feature:

```text
src/web/src/features/<feature>
```

When adding frontend behavior:

1. Keep API calls in a feature API client.
2. Keep reusable state in a context only when multiple screens need it.
3. Prefer existing UI primitives from `src/components/ui`.
4. Keep form validation explicit and user-facing errors actionable.
5. Add Vitest tests for component or state logic when behavior can regress.
6. Add Playwright coverage for important user flows.

## Database Changes

Create a migration with:

```bash
make api-migration name=DescriptiveMigrationName
```

Apply migrations locally:

```bash
make api-migrate
```

Before submitting a database change, verify:

| Check | Reason |
| --- | --- |
| The migration has a clear name | Keeps schema history readable |
| Delete behavior is intentional | Prevents unexpected data loss |
| Indexes match query patterns | Keeps workspace-scoped queries efficient |
| Constraints enforce important invariants | Avoids depending only on application code |

## Testing

Endpoint tests derive from `ApiTest`, which provides an in-memory API and an
`ApiTestClient` (`Api`) for setup and requests:

```csharp
public sealed class ProjectRulesTests(ApiTestApplicationFactory factory)
  : ApiTest(factory)
{
  [Fact]
  public async Task Member_CannotPermanentlyDeleteProject()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken);
    var member = await Api.AddMemberAsync(owner.AccessToken, workspace.Id);
    var project = await Api.CreateProjectAsync(owner.AccessToken, workspace.Id);

    var response = await Api.SendAsync(
      HttpMethod.Delete,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/permanent",
      member.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }
}
```

Response shapes live in `tests/api/Support/ApiContracts.cs`. They describe the
JSON the web client receives, so update them when a contract changes on purpose.
Add a setup helper to `ApiTestClient` when a second test file needs it.

Run focused tests while developing:

```bash
make api-test
make web-test
make web-typecheck
```

Run the full quality gate before opening a pull request:

```bash
make quality
```

If Playwright has not installed Chromium yet:

```bash
pnpm --dir src/web exec playwright install --with-deps chromium
```

## Formatting

Use the project formatters:

```bash
make format
```

Format checks are enforced in CI. Do not hand-format large generated blocks unless the formatter cannot handle them.

## Pull Request Checklist

Before requesting review, confirm:

```text
[ ] The change is focused and explained clearly.
[ ] The change fits the current pre-MVP direction.
[ ] Backend behavior has API or unit tests where appropriate.
[ ] Backend code uses core/shared building blocks instead of local copies.
[ ] Frontend behavior has component, state, or e2e tests where appropriate.
[ ] Database changes include an EF migration.
[ ] Security-sensitive behavior is enforced server-side.
[ ] New public endpoints use AllowAnonymous() and are allow-listed in EndpointAuthorizationTests.
[ ] make quality passes locally.
[ ] Documentation was updated if commands, contracts, or setup changed.
```

## Commit Style

Pull requests are squash-merged, so the **pull request title** becomes the
commit on `master`. CI checks that it follows
[Conventional Commits](https://www.conventionalcommits.org/): a type, an
optional scope, and a lowercase imperative summary without a final period, in
72 characters or fewer. Mark breaking changes with `!` (`feat(api)!: ...`).

Allowed types: `feat`, `fix`, `perf`, `refactor`, `test`, `docs`, `build`,
`ci`, `chore`, `revert`. Use the same style for the commits inside your branch:

```text
feat(boards): add card effort estimate
fix(workspaces): consume invites atomically
refactor(api): move persistence into core
test(api): enforce module conventions
docs: document context-aware planning
```

Keep each commit focused and building on its own. Avoid vague messages such as
`update`, `fix stuff`, or `changes`.

## Reviewing

Reviews should focus on correctness first:

| Priority | Look for |
| --- | --- |
| Behavior | Does the change do what it claims? |
| Security | Are access rules enforced by the API? |
| Tests | Would the tests fail if the behavior regressed? |
| Maintainability | Does the change fit the existing module shape and reuse core/shared? |
| Product feel | Is the user experience clear and consistent? |

Keep feedback specific and actionable.

## Releases

Maintainers release from `master` by pushing a `vX.Y.Z` tag and drafting a
GitHub release from it with generated notes. Only maintainers can create,
move, or delete `v*` tags.
