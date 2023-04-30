FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Dav.AspNetCore.Server.Example/Dav.AspNetCore.Server.Example.csproj", "Dav.AspNetCore.Server.Example/"]
RUN dotnet restore "Dav.AspNetCore.Server.Example/Dav.AspNetCore.Server.Example.csproj"
COPY . .
WORKDIR "/src/Dav.AspNetCore.Server.Example"
RUN dotnet build "Dav.AspNetCore.Server.Example.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Dav.AspNetCore.Server.Example.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dav.AspNetCore.Server.Example.dll"]
