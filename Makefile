


all: test nuget

test: tests/bin/Debug/SQLite.Tests.dll
	nunit-console tests/bin/Debug/SQLite.Tests.dll

tests/bin/Debug/SQLite.Tests.dll: tests/SQLite.Tests.csproj src/SQLite.cs src/SQLiteAsync.cs
	xbuild tests/SQLite.Tests.csproj

nuget: srcnuget pclnuget

srcnuget: src/SQLite.cs src/SQLiteAsync.cs sqlite-net.nuspec
	nuget pack sqlite-net.nuspec

pclnuget: src/SQLite.cs src/SQLiteAsync.cs
	nuget restore SQLite.sln
	'/Applications/Xamarin Studio.app/Contents/MacOS/mdtool' build '-c:Release|iPhone' SQLite.sln -p:SQLite-net
	nuget pack sqlite-net-pcl.nuspec -o .\

