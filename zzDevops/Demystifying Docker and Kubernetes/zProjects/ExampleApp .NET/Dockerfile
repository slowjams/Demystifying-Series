FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ExampleApp.csproj", "."]
RUN dotnet restore "./ExampleApp.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./ExampleApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ExampleApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY wait-for-it /app/wait-for-it.sh
RUN chmod +x /app/wait-for-it.sh
ENV WAITHOST=mysql WAITPORT=3306
ENTRYPOINT ./wait-for-it.sh $WAITHOST:$WAITPORT --timeout=0 && exec dotnet ExampleApp.dll