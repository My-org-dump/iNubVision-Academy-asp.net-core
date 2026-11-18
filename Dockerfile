FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AcademyNet.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /app/App_Data && chown -R app:app /app/App_Data
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
VOLUME ["/app/App_Data"]
ENTRYPOINT ["dotnet", "AcademyNet.dll"]
