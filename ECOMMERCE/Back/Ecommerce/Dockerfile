FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Ecommerce/Ecommerce.csproj Ecommerce/
RUN dotnet restore Ecommerce/Ecommerce.csproj

COPY . .
RUN dotnet publish Ecommerce/Ecommerce.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ecommerce.dll"]
