
SRC=src/SQLite.cs src/SQLiteAsync.cs

PACKAGES_OUT=$(abspath PackagesOut)

all: test nuget

test: tests/bin/Release/SQLite.Tests.dll tests/ApiDiff/bin/Release/ApiDiff.exe
	mono packages/NUnit.ConsoleRunner.3.10.0/tools/nunit3-console.exe tests/bin/Release/SQLite.Tests.dll --labels=On --trace=Info
	mono tests/ApiDiff/bin/Release/ApiDiff.exe

tests/bin/Release/SQLite.Tests.dll: tests/SQLite.Tests.csproj $(SRC)
	nuget restore SQLite.sln
	msbuild /p:Configuration=Release tests/SQLite.Tests.csproj

tests/ApiDiff/bin/Release/ApiDiff.exe: tests/ApiDiff/ApiDiff.csproj $(SRC)
	msbuild /p:Configuration=Release tests/ApiDiff/ApiDiff.csproj

nuget: pclnuget basenuget sqlciphernuget

pclnuget: nuget/SQLite-net-std/SQLite-net-std.csproj $(SRC)
	dotnet pack -c Release -o $(PACKAGES_OUT) $<

basenuget: nuget/SQLite-net-base/SQLite-net-base.csproj $(SRC)
	dotnet pack -c Release -o $(PACKAGES_OUT) $<

sqlciphernuget: nuget/SQLite-net-sqlcipher/SQLite-net-sqlcipher.csproj $(SRC)
	dotnet pack -c Release -o $(PACKAGES_OUT) $<
