FROM mcr.microsoft.com/dotnet/sdk:9.0 as build
WORKDIR /app
EXPOSE 80

# Copy all .csproj and restore as distinct layers. Use of the same Copy command
# for every docker file in the project to take advantage of docker caching
COPY Microservices.sln Microservices.sln
COPY src/AuctionService/AuctionService.csproj src/AuctionService/AuctionService.csproj
COPY src/SearchService/SearchService.csproj src/SearchService/SearchService.csproj
COPY src/GateWayService/GateWayService.csproj src/GateWayService/GateWayService.csproj
COPY src/Contracts/Contracts.csproj src/Contracts/Contracts.csproj
COPY src/IdentitiyService/IdentitiyService.csproj src/IdentitiyService/IdentitiyService.csproj
COPY src/BiddingService/BiddingService.csproj src/BiddingService/BiddingService.csproj


# Restore package deps
RUN dotnet restore Microservices.sln

# Copy the app folder over
COPY src/SearchService src/SearchService
COPY src/Contracts src/Contracts
WORKDIR /app/src/SearchService
RUN dotnet publish -c Release -o /app/src/out

# build run time image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/src/out .
ENTRYPOINT ["dotnet", "SearchService.dll"]