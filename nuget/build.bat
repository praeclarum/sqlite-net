nuget pack sqlite-net.nuspec -o .\

msbuild /p:Configuration=Release SQLite-net\SQLite-net.sln
nuget pack sqlite-net-pcl.nuspec -o .\
