# Plantamoss API (.NET 9) — imagen multi-stage
# 1) build: compila y publica con el SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restaurar primero (capa cacheada mientras no cambie el .csproj)
COPY PlantamossApi.csproj ./
RUN dotnet restore PlantamossApi.csproj

COPY . ./
RUN dotnet publish PlantamossApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# 2) runtime: solo el runtime de ASP.NET (imagen mucho más ligera)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish ./
# Carpeta de logs con permisos para el usuario sin privilegios de la imagen
RUN mkdir -p /app/logs && chown -R $APP_UID /app/logs
# Endurecimiento: la API NO corre como root
USER $APP_UID
ENTRYPOINT ["dotnet", "PlantamossApi.dll"]
