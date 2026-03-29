FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["proto/order.proto", "proto/order.proto"]
COPY ["src/OrderProcessor/Grpc/OrderProcessor.Grpc.csproj", "src/OrderProcessor/Grpc/"]
RUN dotnet restore "src/OrderProcessor/Grpc/OrderProcessor.Grpc.csproj"
COPY ["src/OrderProcessor/Grpc/", "src/OrderProcessor/Grpc/"]
RUN dotnet publish "src/OrderProcessor/Grpc/OrderProcessor.Grpc.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderProcessor.Grpc.dll"]