# .NET Commands And Paths

Load this file when building, testing, or navigating .NET repositories.

## Repository Shape

- Common layouts include a root `.sln`, `src/`, `tests/`, or separate unit/integration test projects.
- Inspect the target repo before assuming paths.

## Build And Test

- Build the relevant solution or project: `dotnet build <path-to.sln-or.csproj>`.
- Run focused tests: `dotnet test <path-to-test.csproj>` when the target is clear.
- Run broader `dotnet test` only when useful and proportionate.
- If no target is clear, ask which solution/project to build or test.
- Prefer focused verification before broad test runs.

## Environment Notes

- Integration tests can depend on Docker/Podman, local services, test containers, or VPN access.
- Build/test/read-only git commands do not need extra user confirmation unless they are unusually broad or environment-mutating.
