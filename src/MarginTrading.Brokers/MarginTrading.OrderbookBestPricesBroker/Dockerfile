FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine3.8
WORKDIR /app
COPY . .
RUN apk add --no-cache icu-libs \
&& cp cert/*.crt /usr/local/share/ca-certificates/ 2>/dev/null \
&& update-ca-certificates \
|| echo "No specific certificate given."
ENTRYPOINT ["dotnet", "MarginTrading.OrderbookBestPricesBroker.dll"]