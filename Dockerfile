FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Struct2Zip/Struct2Zip.csproj Struct2Zip/
RUN dotnet restore Struct2Zip/Struct2Zip.csproj

COPY Struct2Zip/ Struct2Zip/
RUN dotnet publish Struct2Zip/Struct2Zip.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Struct2Zip.dll"]
