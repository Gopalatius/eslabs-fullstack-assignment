FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Assignment.sln", "./"]
COPY ["src/WebSocketApp/WebSocketApp.csproj", "src/WebSocketApp/"]
RUN dotnet restore "src/WebSocketApp/WebSocketApp.csproj"

COPY . .
RUN dotnet publish "src/WebSocketApp/WebSocketApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WebSocketApp.dll"]
