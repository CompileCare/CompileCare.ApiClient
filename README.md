# CompileCare.ApiClient

## Overview

`CompileCare.ApiClient` is a **NuGet-based MSBuild tooling library** that automatically generates **strongly-typed C# API clients** using **NSwag** during build time.

Once installed, any project containing an `nswag.json` file will automatically generate its API client into a `Generated/` folder on build — no manual commands required.

---

## What problems this solves

* No more manual NSwag execution
* Consistent client generation across teams
* DI-friendly HttpClient-based clients
* CI/CD-safe client generation
* Reusable across multiple APIs

---

## High-level architecture

```
CompileCare.ApiClient (NuGet tooling)
  └── build/compilecare.targets   ← MSBuild hook
            ↓
Consumer Project (ApiClientDemo.Client)
  ├── nswag.json                  ← Client definition
  └── Generated/*.cs              ← Auto-generated
```

---

## Folder structure (recommended)

```
CompileCare.ApiClient/
 ├── CompileCare.ApiClient.csproj
 └── build/
     └── compilecare.targets

ApiClientDemo.Client/
 ├── ApiClientDemo.Client.csproj
 ├── nswag.json
 └── Generated/
     └── ApiClientDemoClient.cs   (auto-created)
```

---

## 1. CompileCare.ApiClient.csproj (tooling package)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>

    <PackageId>CompileCare.ApiClient</PackageId>
    <Version>1.0.0</Version>
    <Authors>CompileCare</Authors>
    <Company>CompileCare</Company>
    <Description>NSwag-based API client generation tooling</Description>

    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <None Include="build\\compilecare.targets"
          Pack="true"
          PackagePath="build\\" />
  </ItemGroup>

</Project>
```

---

## 2. build/compilecare.targets (auto-generation hook)

```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

  <Target Name="GenerateCompileCareApiClient"
          BeforeTargets="BeforeCompile"
          Condition="Exists('$(ProjectDir)nswag.json')">

    <Message Text="🚀 CompileCare: Running NSwag client generation"
             Importance="High" />

    <Exec Command="dotnet nswag run &quot;$(ProjectDir)nswag.json&quot;" />

    <Message Text="✅ CompileCare: Client generation completed"
             Importance="High" />

  </Target>

</Project>
```

---

## 3. Consumer project (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CompileCare.ApiClient" Version="1.0.0" />
  </ItemGroup>

</Project>
```

---

## 4. nswag.json (client definition)

```json
{
  "runtime": "Net80",
  "documentGenerator": {
    "fromDocument": {
      "url": "https://localhost:5001/swagger/v1/swagger.json"
    }
  },
  "codeGenerators": {
    "openApiToCSharpClient": {
      "className": "ApiClientDemoClient",
      "namespace": "ApiClientDemo.Client",
      "output": "Generated/ApiClientDemoClient.cs",
      "generateClientInterfaces": true,
      "useHttpClientFactory": true,
      "injectHttpClient": true,
      "generatePartialClasses": true
    }
  }
}
```

> 🔑 The `output` property ensures the `Generated` folder is created automatically.

---

## 5. Required dependency

NSwag must be available on the machine or CI agent:

```bash
dotnet tool install --global NSwag.ConsoleCore
```

---

## Build flow

1. `dotnet build` starts
2. MSBuild imports `compilecare.targets`
3. Target detects `nswag.json`
4. NSwag runs automatically
5. `Generated/*.cs` is created

---

## Troubleshooting

### ❌ Generated folder not created

* Ensure `nswag.json` is at project root
* Ensure NSwag tool is installed
* Check build output for "CompileCare:" messages

### ❌ Swagger URL unreachable

* Ensure API is running
* Or switch to file-based swagger generation

---

## CI/CD notes

* Install NSwag tool in pipeline
* Ensure API swagger endpoint is reachable
* Cache NuGet packages for faster builds

---

## FAQ

**Q: Can this support multiple APIs?**
Yes — one `nswag.json` per client project.

**Q: Can I disable generation?**
Yes — wrap target with a custom MSBuild property.

---

## Summary

✔ Zero manual client generation
✔ Clean separation of API vs client
✔ Enterprise-ready tooling
✔ Reusable across teams

---

Happy coding 🚀
