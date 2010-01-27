
DIST = sqlite-net-$(VER)

CP = cp -Rf
RM = rm -Rf

all: examples tests	

examples:
	xbuild examples/Stocks/Stocks.csproj

tests: SQLite.Tests.dll
	nunit-console2 SQLite.Tests.dll

SQLite.Tests.dll: tests/BooleanTest.cs src/SQLite.cs
	gmcs tests/BooleanTest.cs src/SQLite.cs -r:NUnit.Framework -target:library -out:SQLite.Tests.dll

dist:
	rm -Rf $(DIST)
	rm -Rf $(DIST).zip
	mkdir $(DIST)
	cp -Rf src/SQLite.cs $(DIST)/
	cp -Rf src/SQLite.MonoTouchAdmin.cs $(DIST)/
	cp -Rf readme.txt $(DIST)/
	rm -Rf $(DIST)/src/.svn
	cp -Rf examples $(DIST)/
	rm -Rf $(DIST)/examples/.svn
	rm -Rf $(DIST)/examples/Stocks/.svn
	rm -Rf $(DIST)/examples/Stocks/bin
	rm -Rf $(DIST)/examples/Stocks/obj
	rm -Rf $(DIST)/examples/Stocks/*.pidb
	rm -Rf $(DIST)/examples/Stocks/*.userprefs
	rm -Rf $(DIST)/examples/StocksTouch/.svn
	rm -Rf $(DIST)/examples/StocksTouch/bin
	rm -Rf $(DIST)/examples/StocksTouch/obj
	rm -Rf $(DIST)/examples/StocksTouch/*.pidb
	rm -Rf $(DIST)/examples/StocksTouch/*.userprefs
	rm -Rf $(DIST)/.DS_Store
	zip -9 -r $(DIST).zip $(DIST)
	rm -Rf $(DIST)
