FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["TacticalThievesServer.csproj", "./"]
RUN dotnet restore "TacticalThievesServer.csproj"

COPY . .

RUN dotnet publish "TacticalThievesServer.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

#certificat HTTPS
COPY ./https/aspnetapp.pfx /https/aspnetapp.pfx

ENV ASPNETCORE_URLS=https://+:7186
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=password123

EXPOSE 7186

#EXPOSE 80
#EXPOSE 443

ENTRYPOINT ["dotnet", "TacticalThievesServer.dll"]