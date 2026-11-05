FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Manufacturing.slnx
RUN dotnet publish src/Manufacturing.Api -c Release --no-restore -o /app
FROM mcr.microsoft.com/dotnet/aspnet:10.0.14
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Manufacturing.Api.dll"]
