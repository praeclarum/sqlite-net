mkdir SQLite.Net
copy /y ..\src\SQLite.Net\bin\Release\SQLite.Net.dll SQLite.Net
type nul >SQLite.Net\_._

mkdir SQLite.Net.Async
copy /y ..\src\SQLite.Net.Async\bin\Release\SQLite.Net.Async.dll SQLite.Net.Async

mkdir SQLite.Net.Platform.Generic
copy /y ..\src\SQLite.Net.Platform.Generic\bin\Release\SQLite.Net.Platform.Generic.dll SQLite.Net.Platform.Generic

mkdir SQLite.Net.Platform.Win32
copy /y ..\src\SQLite.Net.Platform.Win32\bin\Release\SQLite.Net.Platform.Win32.dll SQLite.Net.Platform.Win32

mkdir SQLite.Net.Platform.WindowsPhone8
mkdir SQLite.Net.Platform.WindowsPhone8\ARM
mkdir SQLite.Net.Platform.WindowsPhone8\x86
copy /y ..\src\SQLite.Net.Platform.WindowsPhone8\bin\x86\Release\SQLite.Net.Platform.WindowsPhone8.dll SQLite.Net.Platform.WindowsPhone8\x86
copy /y ..\src\SQLite.Net.Platform.WindowsPhone8\bin\ARM\Release\SQLite.Net.Platform.WindowsPhone8.dll SQLite.Net.Platform.WindowsPhone8\ARM

mkdir SQLite.Net.Platform.WinRT
copy /y ..\src\SQLite.Net.Platform.WinRT\bin\Release\SQLite.Net.Platform.WinRT.dll SQLite.Net.Platform.WinRT

mkdir SQLite.Net.Platform.XamarinAndroid
copy /y ..\src\SQLite.Net.Platform.XamarinAndroid\bin\Release\SQLite.Net.Platform.XamarinAndroid.dll SQLite.Net.Platform.XamarinAndroid

mkdir SQLite.Net.Platform.XamarinIOS
copy /y ..\src\SQLite.Net.Platform.XamarinIOS\bin\iPhone\Release\SQLite.Net.Platform.XamarinIOS.dll SQLite.Net.Platform.XamarinIOS

mkdir SQLite.Net.Platform.XamarinIOS.Unified
copy /y ..\src\SQLite.Net.Platform.XamarinIOS.Unified\bin\Release\SQLite.Net.Platform.XamarinIOS.Unified.dll SQLite.Net.Platform.XamarinIOS.Unified
