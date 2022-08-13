set prompt=$G

dotnet run -p Client.csproj < nul
pause

dotnet run -p Client.csproj < tt-test.txt
pause

dotnet run -p Client.csproj < tt-hamlet.txt
pause

dotnet run -p Client.csproj < tt-casablanca.txt
pause

dotnet run -p Client.csproj < tt-dylan.txt
pause

dotnet run -p Client.csproj < tt-leonard.txt
pause

dotnet run -p Client.csproj < tt-dante.txt
pause
pause
