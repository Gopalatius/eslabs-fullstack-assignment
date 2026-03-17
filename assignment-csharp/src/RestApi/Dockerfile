FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Assignment.sln", "./"]
COPY ["src/RestApi/RestApi.csproj", "src/RestApi/"]
RUN dotnet restore "src/RestApi/RestApi.csproj"

COPY . .
RUN dotnet publish "src/RestApi/RestApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RestApi.dll"]
