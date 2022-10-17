FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY . .
RUN apk add --no-cache icu-libs \
&& apk add --no-cache tzdata \
&& cp cert/*.crt /usr/local/share/ca-certificates/ 2>/dev/null \
&& update-ca-certificates \
|| echo "No specific certificate given."
RUN apk update && apk add bash
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENTRYPOINT ["dotnet", "MarginTrading.Backend.dll"]