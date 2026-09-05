# Third-Party Notices

ADWAIS is distributed under the MIT License (see [LICENSE](./LICENSE)).
This document lists the third-party open-source components used by the project and
their licenses, in accordance with their license terms.

## .NET / NuGet

| Package | Version | License | License text |
|---|---|---|---|
| Microsoft.* (EF Core, ASP.NET Core, DataProtection, JwtBearer, HttpClient/Resilience, OpenApi, IdentityModel.Tokens.Jwt, System.* packages) | 10.0.x / 8.x / 10.8.0 | MIT | [.NET Library License](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) |
| Hangfire.AspNetCore | 1.8.24 | **LGPL-3.0** | [Hangfire LICENSE.md](https://raw.githubusercontent.com/HangfireIO/Hangfire/master/LICENSE.md) |
| Hangfire.PostgreSql | 1.21.1 | **LGPL-3.0** | [Hangfire.PostgreSql LICENSE](https://github.com/frankhommers/Hangfire.PostgreSql/blob/main/LICENSE) |
| Swashbuckle.AspNetCore | 10.2.3 | MIT | [Swashbuckle LICENSE](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/LICENSE) |
| FluentValidation (+ DependencyInjectionExtensions) | 12.1.1 | Apache-2.0 | [FluentValidation LICENSE](https://github.com/FluentValidation/FluentValidation/blob/main/LICENSE) |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | PostgreSQL License | [efcore.pg LICENSE](https://github.com/npgsql/efcore.pg/blob/main/LICENSE) |
| Newtonsoft.Json | 13.0.4 | MIT | [Newtonsoft.Json LICENSE](https://raw.githubusercontent.com/JamesNK/Newtonsoft.Json/master/LICENSE.md) |
| HtmlAgilityPack | 1.12.4 | MIT | [HtmlAgilityPack LICENSE](https://github.com/zzzprojects/html-agility-pack/blob/master/LICENSE) |
| Ical.Net | 5.2.3 | MIT | [ical.net LICENSE](https://github.com/rianjs/ical.net/blob/master/LICENSE.txt) |
| Cronos | 0.13.0 | MIT | [Cronos LICENSE](https://github.com/HangfireIO/Cronos/blob/master/LICENSE.md) |
| DotNetEnv | 3.2.0 | MIT | [dotnet-env LICENSE](https://github.com/tonerdo/dotnet-env/blob/master/LICENSE) |
| EFCore.NamingConventions | 10.0.1 | MIT | [EFCore.NamingConventions LICENSE](https://github.com/efcore/EFCore.NamingConventions/blob/main/LICENSE) |

### Test-only

| Package | Version | License | License text |
|---|---|---|---|
| Moq | 4.20.72 | BSD-3-Clause | [Moq LICENSE](https://github.com/devlooped/moq/blob/main/LICENSE) |
| xunit (+ runner.visualstudio) | 2.9.3 / 3.1.5 | Apache-2.0 | [xunit LICENSE](https://github.com/xunit/xunit/blob/main/LICENSE) |
| coverlet.collector | 10.0.1 | MIT | [coverlet LICENSE](https://github.com/coverlet-coverage/coverlet/blob/master/LICENSE) |
| Microsoft.NET.Test.Sdk, Microsoft.EntityFrameworkCore.InMemory | 18.8.1 / 10.0.10 | MIT | [.NET Library License](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) |

## Frontend (npm / pnpm)

| Package | Version | License | License text |
|---|---|---|---|
| react, react-dom | ^19.2.4 | MIT | [React LICENSE](https://github.com/facebook/react/blob/main/LICENSE) |
| @tanstack/react-query, react-router, react-query-devtools, router-vite-plugin, eslint-plugin-query | ^5.x / ^1.x | MIT | [TanStack LICENSE](https://github.com/TanStack/query/blob/main/LICENSE) |
| chart.js | ^4.5.1 | MIT | [Chart.js LICENSE](https://github.com/chartjs/Chart.js/blob/master/LICENSE.md) |
| lucide-react | ^1.16.0 | ISC | [Lucide LICENSE](https://github.com/lucide-icons/lucide/blob/main/LICENSE) |
| oidc-client-ts | ^3.1.0 | Apache-2.0 | [oidc-client-ts LICENSE](https://github.com/authts/oidc-client-ts/blob/main/LICENSE) |
| react-oidc-context | ^3.2.0 | MIT | [react-oidc-context LICENSE](https://github.com/authts/react-oidc-context/blob/main/LICENSE) |
| sonner | ^2.0.7 | MIT | [sonner LICENSE](https://github.com/emilkowalski/sonner/blob/main/LICENSE.md) |
| zod | ^4.4.3 | MIT | [zod LICENSE](https://github.com/colinhacks/zod/blob/main/LICENSE) |
| pg (repo-root tooling) | ^8.20.0 | MIT | [node-postgres LICENSE](https://github.com/brianc/node-postgres/blob/master/LICENSE) |

Dev tooling (vite, vitest, eslint, typescript, tailwindcss, postcss, autoprefixer, orval, jsdom, testing-library, @vitejs/plugin-react, globals, etc.) is used at build/test time only; all are MIT or Apache-2.0 and are not part of distributed bundles beyond what their respective terms permit.

## LGPL-3.0 Notice (Hangfire)

Hangfire and Hangfire.PostgreSql are distributed under the GNU Lesser General Public
License v3.0 (LGPL-3.0). ADWAIS uses these libraries **unmodified**, as dynamically
linked libraries obtained from NuGet, and provides no modifications to them. Per the
LGPL, users of ADWAIS are free to replace the Hangfire assemblies with their own
versions. The full LGPL-3.0 license text is available at
https://www.gnu.org/licenses/lgpl-3.0.html and in the links above.

## Notes

- License texts are the authoritative sources; links may change upstream. Refer to
  the linked files or the package registries (nuget.org / npm) for the current text.
- This list covers direct dependencies declared in the project manifests
  (`*.csproj`, `package.json`) and the lock files (`packages.lock.json`,
  `pnpm-lock.yaml`) pin their transitive versions.
