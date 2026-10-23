# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/services/ShoppingCartService/ShoppingCart.Core/ShoppingCart.Domain/ShoppingCart.Domain.csproj src/services/ShoppingCartService/ShoppingCart.Core/ShoppingCart.Domain/
COPY src/services/ShoppingCartService/ShoppingCart.Core/ShoppingCart.Application/ShoppingCart.Application.csproj src/services/ShoppingCartService/ShoppingCart.Core/ShoppingCart.Application/
COPY src/services/ShoppingCartService/ShoppingCart.Infrastructure/ShoppingCart.Persistence/ShoppingCart.Persistence.csproj src/services/ShoppingCartService/ShoppingCart.Infrastructure/ShoppingCart.Persistence/
COPY src/services/ShoppingCartService/ShoppingCart.Infrastructure/ShoppingCart.Infrastructure/ShoppingCart.Infrastructure.csproj src/services/ShoppingCartService/ShoppingCart.Infrastructure/ShoppingCart.Infrastructure/
COPY src/services/ShoppingCartService/ShoppingCart.API/ShoppingCart.API.csproj src/services/ShoppingCartService/ShoppingCart.API/
RUN dotnet restore src/services/ShoppingCartService/ShoppingCart.API/ShoppingCart.API.csproj

COPY src/ src/
RUN dotnet publish src/services/ShoppingCartService/ShoppingCart.API/ShoppingCart.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Non-root user provided by the .NET base images.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ShoppingCart.API.dll"]
