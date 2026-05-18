ARG sdkTag=8.0.421
ARG aspNetTag=8.0
FROM mcr.microsoft.com/dotnet/sdk:${sdkTag} AS build-env

WORKDIR /app

COPY . ./

RUN dotnet nuget add source /app/packages --name Local

WORKDIR /app
RUN ls
RUN dotnet build -c Release -o /app/out
# RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:${aspNetTag}

WORKDIR /app

# Copy the output from the build environment to the container
COPY --from=build-env /app/out ./

EXPOSE 80

ENTRYPOINT ["dotnet", "minimal-api.dll"]
