This folder is for testing against different versions of the .NET SDK in a container

I found people with older versions of the .NET 6 SDK would get an error message like this:

```plaintext
CSC : warning CS8032: An instance of analyzer Seekatar.OptionToStringGenerator.OptionPropertyToStringGenerator cannot be created from /root/.nuget/packages/seekatar.optiontostringgenerator/0.3.0/analyzers/dotnet/cs/Seekatar.OptionToStringGenerator.dll : Could not load file or assembly 'Microsoft.CodeAnalysis, Version=4.3.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'. The system cannot find the file specified.. [/app/minimal-api.csproj]
```

To fix these, I changed generator csproj to use `Microsoft.CodeAnalysis.CSharp` 4.3.0 from 4.6.0 and don't get for .NET 6.0.416 or higher.

```powershell
docker build --tag testapi --build-arg sdk=6.0.202 .
docker run --rm -p 7175:80 testapi
irm http://localhost:7175/weatherforecast
```

The TFM is `net6.0` in the csproj

| SDK Version in Dockerfile | Status                                                                   |
| ------------------------- | ------------------------------------------------------------------------ |
| 6.0.202                   | Could not load file or assembly 'Microsoft.CodeAnalysis, Version=4.3.0.0 |
| 6.0.416                   | Builds
| 7.0.201                   | builds if use 4.4.0 of analyzer nupkg                                                      |
| 7.0.306                   | builds                                                                   |
| 7.0.403                   | builds                                                                   |