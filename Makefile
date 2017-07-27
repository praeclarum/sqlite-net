
SRC=src/SQLite.cs src/SQLiteAsync.cs

all: test nuget

test: tests/bin/Debug/SQLite.Tests.dll
	nunit-console tests/bin/Debug/SQLite.Tests.dll

tests/bin/Debug/SQLite.Tests.dll: tests/SQLite.Tests.csproj $(SRC)
	msbuild tests/SQLite.Tests.csproj

nuget: srcnuget pclnuget

packages: nuget/SQLite-net/packages.config
	nuget restore SQLite.sln

srcnuget: sqlite-net.nuspec $(SRC)
	nuget pack sqlite-net.nuspec

pclnuget: sqlite-net-pcl.nuspec packages $(SRC)
	msbuild "/p:Configuration=Release" nuget/SQLite-net/SQLite-net.csproj
	msbuild "/p:Configuration=Release" nuget/SQLite-net-std/SQLite-net-std.csproj 
	nuget pack sqlite-net-pcl.nuspec -o .\

