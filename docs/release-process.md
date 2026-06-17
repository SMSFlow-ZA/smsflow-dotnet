# Release process

Use this checklist when publishing a new `SmsFlow` version.

## Before release

1. Update `src/SmsFlow/SmsFlow.csproj` version.
2. Update `CHANGELOG.md`.
3. Run `dotnet test tests/SmsFlow.Tests/SmsFlow.Tests.csproj --configuration Release`.
4. Run `dotnet pack src/SmsFlow/SmsFlow.csproj --configuration Release --output artifacts`.
5. Confirm no credentials, customer data, logs, or private URLs are present.

## Publish

1. Merge to `main` after CI passes.
2. Create a GitHub release named `vX.Y.Z`.
3. The publish workflow publishes `SmsFlow` to NuGet.
4. The package smoke workflow installs the published package from NuGet.

## Token rotation

The NuGet API key is stored as the GitHub Actions secret `NUGET_API_KEY`.

Create a new NuGet API key under the verified `SMSFlow` owner with `Push new packages and package versions` permission and a package glob that covers the package ID. Replace the GitHub secret immediately after rotation.

