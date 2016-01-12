


all: test	

test: tests/bin/Debug/SQLite.Tests.dll
	nunit-console tests/bin/Debug/SQLite.Tests.dll

tests/bin/Debug/SQLite.Tests.dll:
	xbuild tests/SQLite.Tests.csproj

