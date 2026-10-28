## What and why

<!-- What does this change do, and why is it needed? Link the issue if there is one. -->

## How to verify

<!-- Commands, requests, or screens a reviewer can use to check the behavior. -->

## Checklist

- [ ] `make quality` passes locally
- [ ] Behavior is covered by tests that would fail without this change
- [ ] Backend code reuses `core`/`shared` (results, validation, `CurrentUser`, `TimeProvider`) instead of local copies
- [ ] New public endpoints use `AllowAnonymous()` and are allow-listed in `EndpointAuthorizationTests`
- [ ] Access rules are enforced by the API, not only in the UI
- [ ] Database changes include an EF Core migration
- [ ] Docs are updated if commands, contracts, or setup changed
