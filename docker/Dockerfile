FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/PotteryJournal.SharedKernel/PotteryJournal.SharedKernel.csproj src/PotteryJournal.SharedKernel/
COPY src/PotteryJournal.Infrastructure/PotteryJournal.Infrastructure.csproj src/PotteryJournal.Infrastructure/
COPY src/PotteryJournal.Web/PotteryJournal.Web.csproj src/PotteryJournal.Web/
RUN dotnet restore src/PotteryJournal.Web/PotteryJournal.Web.csproj

COPY src/ src/
RUN dotnet publish src/PotteryJournal.Web/PotteryJournal.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PotteryJournal.Web.dll"]
