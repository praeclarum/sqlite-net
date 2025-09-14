
SRC=src/SQLite.cs src/SQLiteAsync.cs

PACKAGES_OUT=$(abspath PackagesOut)

all: nuget

nuget: sourcegenerator pclnuget basenuget sqlciphernuget staticnuget

sourcegenerator: nuget\Sqlite_net.SourceGenerator\Sqlite_net.SourceGenerator.csproj $(SRC)
	dotnet build -c Release $<

pclnuget: nuget/SQLite-net-std/SQLite-net-std.csproj $(SRC)
	dotnet pack -c Release -o $(PACKAGES_OUT) $<

basenuget: nuget/SQLite-net-base/SQLite-net-base.csproj $(SRC)
	dotnet pack -c Release -o $(PACKAGES_OUT) $<

sqlciphernuget: nuget/SQLite-net-sqlcipher/SQLite-net-sqlcipher.csproj $(SRC)
	dotnet pack -c Release -o $(PACKAGES_OUT) $<

staticnuget: nuget/SQLite-net-static/SQLite-net-static.csproj $(SRC)
	dotnet pack -c Release -o $(PACKAGES_OUT) $<

codecoverage:
	cd tests/SQLite.Tests && dotnet test /p:AltCover=true /p:AltCoverForce=true "/p:AltCoverTypeFilter=SQLite.Tests.*" && reportgenerator -reports:coverage.xml -targetdir:./CoverageReport
