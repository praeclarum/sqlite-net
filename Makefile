


all: test nuget

test: tests/bin/Debug/SQLite.Tests.dll
	nunit-console tests/bin/Debug/SQLite.Tests.dll

tests/bin/Debug/SQLite.Tests.dll: tests/SQLite.Tests.csproj src/SQLite.cs src/SQLiteAsync.cs
	xbuild tests/SQLite.Tests.csproj

nuget: srcnuget pclnuget

srcnuget: src/SQLite.cs src/SQLiteAsync.cs sqlite-net.nuspec
	nuget pack sqlite-net.nuspec

pclnuget: src/SQLite.cs src/SQLiteAsync.cs
	xbuild /p:Configuration=Release nuget/SQLite-net/SQLite-net.sln
	nuget pack sqlite-net-pcl.nuspec -o .\

