This folder is for testing against different versions of the .NET SDK in a container

I found people with older versions of the .NET 6 SDK would get an error message like this:

```plaintext
CSC : warning CS8032: An instance of analyzer Seekatar.OptionToStringGenerator.OptionPropertyToStringGenerator cannot be created from /root/.nuget/packages/seekatar.optiontostringgenerator/0.3.0/analyzers/dotnet/cs/Seekatar.OptionToStringGenerator.dll : Could not load file or assembly 'Microsoft.CodeAnalysis, Version=4.3.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'. The system cannot find the file specified.. [/app/minimal-api.csproj]
```

To fix these, I changed generator csproj to use `Microsoft.CodeAnalysis.CSharp` 4.3.0 from 4.6.0 and don't get for .NET 6.0.416 or higher.

Later I found if the versions of these were too high, then you'd get errors about the compiler version not found. I bumped these down since 4.14 forced used to have .NET 10 installed. Also `Attributes.csproj` has a reference to `System.Text.Json 9.x` which caused an **exception at runtime** when running on .NET 8.

```xml
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.11.0" PrivateAssets="all" />
```

See [test-container/InvokeApi.Tests.ps1](InvokeApi.Tests.ps1) for details about how the tests are run. `./run.ps1 testBuilds` will run the tests. Best to do pack first `./run.ps1 pack,testBuilds -Vers 0.3.7.3`

The TFM is `net8.0` in the csproj for the minimal app.

| SDK Version in Dockerfile | Status       |
| ------------------------- | ------------ |
| 8.0.421                   | builds, runs |
| 9.0.314                   | builds, runs |
| 10.0.300                  | builds, runs |