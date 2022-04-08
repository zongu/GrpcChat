FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore ./GrpcChat.Server
RUN dotnet dev-certs https -ep GrpcChat.Server.pfx -p 123456
RUN dotnet dev-certs https --trust
RUN dotnet publish "GrpcChat.Server/GrpcChat.Server.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
RUN mkdir LogFiles
RUN chmod 777 LogFiles
EXPOSE 8085
COPY --from=build /src/out ./
COPY --from=build /src/GrpcChat.Server.pfx ./
ENTRYPOINT ["dotnet", "GrpcChat.Server.dll"]