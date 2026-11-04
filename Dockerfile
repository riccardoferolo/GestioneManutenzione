FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Manutenzione.Api/Manutenzione.Api.csproj src/Manutenzione.Api/
RUN dotnet restore src/Manutenzione.Api/Manutenzione.Api.csproj
COPY src/ src/
RUN dotnet publish src/Manutenzione.Api/Manutenzione.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Manutenzione.Api.dll"]
