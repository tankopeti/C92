FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /src
COPY Cloud9.2.csproj .
RUN dotnet restore "Cloud9.2.csproj"
COPY . .
COPY cert.pfx /https/cert.pfx
RUN dotnet tool install --global dotnet-watch
ENV PATH="$PATH:/root/.dotnet/tools"
EXPOSE 8080
ENV ASPNETCORE_URLS=https://+:8080
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/cert.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=HUmmer512!!!
CMD ["dotnet", "watch", "run", "--project", "Cloud9.2.csproj", "--urls", "https://+:8080"]