FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
WORKDIR /app
EXPOSE 80

# copy all .csproj files and restore as distinct layers. 
COPY Identity.API/Identity.API.csproj Identity.API/Identity.API.csproj

# Restore package dependecies
RUN dotnet restore Identity.API/Identity.API.csproj

# Copy the micro service folders over
COPY Identity.API Identity.API
WORKDIR /app/Identity.API
RUN dotnet publish -c Release -o /app/src/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/src/out .
ENTRYPOINT [ "dotnet", "Identity.API.dll" ]