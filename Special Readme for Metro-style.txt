Special readme note for Metro-style
---------------------------------------

You will need to add a link into sqlite3.dll to this project in order to run it
under Metro-style.

SQLite.org have a special branch specifically for WinRT. You will need a special
WinRT capable version of SQLite to run it.

You can find one at:
https://github.com/mbrit/sqlite-metrostyle

Download the ~/bin/sqlite3.dll from the repo. Then add it to the project as 
a *file* not as an assembly reference. Right-click on the project, select
"Add - Existing Item". Make sure the file is set as "Content". This will cause
the XAML packager to include the project. This needs to be done for the 
test library project and the demo app project.
