# docker build --tag testapi .
# docker run -p 7175:80 testapi
# irm http://localhost:7175/weatherforecast

# Use the .NET SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:7.0.101 AS build-env

# Set the working directory to /app
WORKDIR /app

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
