FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /src

FROM base AS deps
COPY ["src/AssistenteExecutivo.Api/AssistenteExecutivo.Api.csproj", "src/AssistenteExecutivo.Api/"]
COPY ["src/AssistenteExecutivo.Application/AssistenteExecutivo.Application.csproj", "src/AssistenteExecutivo.Application/"]
COPY ["src/AssistenteExecutivo.Infrastructure/AssistenteExecutivo.Infrastructure.csproj", "src/AssistenteExecutivo.Infrastructure/"]
COPY ["src/AssistenteExecutivo.Domain/AssistenteExecutivo.Domain.csproj", "src/AssistenteExecutivo.Domain/"]
RUN dotnet restore "src/AssistenteExecutivo.Api/AssistenteExecutivo.Api.csproj"

FROM base AS builder
COPY --from=deps /src ./
COPY . .
WORKDIR "/src/src/AssistenteExecutivo.Api"
RUN dotnet build "AssistenteExecutivo.Api.csproj" -c Release -o /app/build

FROM builder AS publish
RUN dotnet publish "AssistenteExecutivo.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runner
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AssistenteExecutivo.Api.dll"]

