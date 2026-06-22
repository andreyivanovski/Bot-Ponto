FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY BotPonto.sln ./
COPY src/BotPonto/BotPonto.csproj src/BotPonto/
RUN dotnet restore src/BotPonto/BotPonto.csproj
COPY src/BotPonto/ src/BotPonto/
RUN dotnet publish src/BotPonto/BotPonto.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
ENV TZ=America/Sao_Paulo
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BotPonto.dll"]
