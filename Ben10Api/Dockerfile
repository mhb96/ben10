FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Ben10Api/Ben10Api.csproj", "Ben10Api/"]
RUN dotnet restore "Ben10Api/Ben10Api.csproj"
COPY Ben10Api/ Ben10Api/
RUN dotnet publish "Ben10Api/Ben10Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ben10Api.dll"]
