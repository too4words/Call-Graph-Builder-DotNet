
md c:\temp
md c:\temp\solutions2

echo External\AzCopy\AzCopy.exe /Dest:C:\Temp\solutions2 /Source:%Source% /SourceKey:%SourceKey%  /SourceType:Blob /S /Y /pattern:* >> c:\Temp\sal.txt


External\AzCopy\AzCopy.exe /Dest:C:\Temp\solutions2 /Source:%Source% /SourceKey:%SourceKey%  /SourceType:Blob /S /Y /pattern:* 

EXIT /B 0