FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY . .
RUN apk add --no-cache icu-libs \
&& cp cert/*.crt /usr/local/share/ca-certificates/ 2>/dev/null \
&& update-ca-certificates \
|| echo "No specific certificate given."
ENTRYPOINT ["dotnet", "MarginTrading.Backend.dll"]