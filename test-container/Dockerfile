ARG sdkTag=6.0.416
FROM mcr.microsoft.com/dotnet/sdk:${sdkTag} AS build-env

WORKDIR /app

COPY minimal-api.csproj .

RUN dotnet restore ./minimal-api.csproj

COPY . ./

WORKDIR /app
RUN ls
RUN dotnet build -c Release -o /app/out
# RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0

WORKDIR /app

# Copy the output from the build environment to the container
COPY --from=build-env /app/out ./

EXPOSE 80

ENTRYPOINT ["dotnet", "minimal-api.dll"]
