FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RaceDay.API/RaceDay.API.csproj RaceDay.API/
RUN dotnet restore RaceDay.API/RaceDay.API.csproj
COPY RaceDay.API/ RaceDay.API/
RUN dotnet publish RaceDay.API/RaceDay.API.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.API.dll"]
