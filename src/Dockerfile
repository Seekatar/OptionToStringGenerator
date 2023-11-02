
# Use the .NET SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:7.0.101 AS build-env

# Set the working directory to /app
WORKDIR /app

# Copy the .csproj file to the container
COPY OptionToStringGenerator/OptionToStringGenerator.csproj ./OptionToStringGenerator/
COPY Attributes/Attributes.csproj ./Attributes/

# Restore the NuGet packages
RUN dotnet restore ./OptionToStringGenerator/OptionToStringGenerator.csproj

# Copy the rest of the application code to the container
COPY . ./

# Build the application
WORKDIR /app/OptionToStringGenerator
RUN ls
RUN dotnet build -c Release -o /app/out
# RUN dotnet publish -c Release -o out

# Use the .NET runtime image as the base image
FROM mcr.microsoft.com/dotnet/runtime:7.0.1

# Set the working directory to /app
WORKDIR /app

# Copy the output from the build environment to the container
COPY --from=build-env /app/out ./

# Set the entry point for the container
ENTRYPOINT ["dotnet", "OptionToStringGenerator.dll"]
