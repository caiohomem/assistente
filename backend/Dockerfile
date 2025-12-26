FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /src

FROM base AS deps
COPY ["src/AssistenteExecutivo.Api/AssistenteExecutivo.Api.csproj", "AssistenteExecutivo.Api/"]
COPY ["src/AssistenteExecutivo.Application/AssistenteExecutivo.Application.csproj", "AssistenteExecutivo.Application/"]
COPY ["src/AssistenteExecutivo.Infrastructure/AssistenteExecutivo.Infrastructure.csproj", "AssistenteExecutivo.Infrastructure/"]
COPY ["src/AssistenteExecutivo.Domain/AssistenteExecutivo.Domain.csproj", "AssistenteExecutivo.Domain/"]
RUN dotnet restore "AssistenteExecutivo.Api/AssistenteExecutivo.Api.csproj"

FROM base AS builder
COPY --from=deps /src ./
COPY src/AssistenteExecutivo.Api/ AssistenteExecutivo.Api/
COPY src/AssistenteExecutivo.Application/ AssistenteExecutivo.Application/
COPY src/AssistenteExecutivo.Infrastructure/ AssistenteExecutivo.Infrastructure/
COPY src/AssistenteExecutivo.Domain/ AssistenteExecutivo.Domain/
WORKDIR "/src/AssistenteExecutivo.Api"
RUN dotnet build "AssistenteExecutivo.Api.csproj" -c Release -o /app/build

FROM builder AS publish
WORKDIR "/src/AssistenteExecutivo.Api"
RUN dotnet publish "AssistenteExecutivo.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runner
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AssistenteExecutivo.Api.dll"]

