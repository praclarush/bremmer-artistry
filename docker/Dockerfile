FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/PotteryJournal.SharedKernel/PotteryJournal.SharedKernel.csproj src/PotteryJournal.SharedKernel/
COPY src/PotteryJournal.Infrastructure/PotteryJournal.Infrastructure.csproj src/PotteryJournal.Infrastructure/
COPY src/PotteryJournal.Web/PotteryJournal.Web.csproj src/PotteryJournal.Web/
RUN dotnet restore src/PotteryJournal.Web/PotteryJournal.Web.csproj

# Dev target: no source baked in, no publish -- src/ is bind-mounted at runtime (see
# docker-compose.dev.yml) and dotnet watch recompiles on change. Packages are already
# restored above so the bind mount doesn't force a slow first-run restore.
FROM build AS dev
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "watch", "run", "--project", "src/PotteryJournal.Web/PotteryJournal.Web.csproj", "--no-launch-profile", "--non-interactive"]

FROM build AS publish
COPY src/ src/
RUN dotnet publish src/PotteryJournal.Web/PotteryJournal.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PotteryJournal.Web.dll"]
