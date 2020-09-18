FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build

ENV PROJECT_PATH=SFA.DAS.Tools.ServiceBus.Support.Web/SFA.DAS.Tools.ServiceBus.Support.Web.csproj
COPY ./src ./src
WORKDIR /src

RUN dotnet restore $PROJECT_PATH
RUN dotnet build $PROJECT_PATH -c release --no-restore
RUN dotnet publish $PROJECT_PATH -c release --no-build -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SFA.DAS.Tools.Support.Web.dll"]
