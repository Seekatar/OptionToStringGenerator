# docker build --tag testapi .
# docker run --rm -p 7175:80 testapi
# irm http://localhost:7175/weatherforecast

# Use the .NET SDK image as the base image

# Justin error about 4.4 but want 4.6 of compiler
# UPDATE -- changed generator csproj to Microsoft.CodeAnalysis.CSharp 4.4.0 and don't get
#           error, Alan has this version, gets a not found error on Microsoft.CodeAnalysis
# builds if use 4.3.0
FROM mcr.microsoft.com/dotnet/sdk:6.0.416 AS build-env

# builds if use 4.4.0
# FROM mcr.microsoft.com/dotnet/sdk:7.0.201 AS build-env

# builds
# FROM mcr.microsoft.com/dotnet/sdk:7.0.306 AS build-env

# builds
# FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
# builds
# FROM mcr.microsoft.com/dotnet/sdk:7.0.403 AS build-env

# Set the working directory to /app
WORKDIR /app

COPY packages packages

RUN dotnet nuget add source /app/packages --name Local

# Copy the .csproj file to the container
COPY minimal-api.csproj .

# Restore the NuGet packages
RUN dotnet restore ./minimal-api.csproj

# Copy the rest of the application code to the container
COPY . ./

# Build the application
WORKDIR /app
RUN ls
RUN dotnet build -c Release -o /app/out
# RUN dotnet publish -c Release -o out

# Use the .NET runtime image as the base image
FROM mcr.microsoft.com/dotnet/aspnet:6.0

# Set the working directory to /app
WORKDIR /app

# Copy the output from the build environment to the container
COPY --from=build-env /app/out ./

EXPOSE 80

# Set the entry point for the container
ENTRYPOINT ["dotnet", "minimal-api.dll"]
