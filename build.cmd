REM 

msbuild.exe /property:WixSdkPath=c:\wix\sdk\ /property:WixTasksPath=c:\wix\WixTasks.dll /property:WixCATargetsPath=c:\wix\SDK\wix.ca.targets /property:WixTargetsPath=c:\wix\wix.targets /property:WixTasksPath=c:\wix\WixTasks.dll /property:WixToolPath=c:\wix\  WixDataBase.Setup.sln  /t:clean

REM msbuild.exe /property:WixSdkPath=c:\wix\sdk\ /property:WixTasksPath=c:\wix\WixTasks.dll /property:WixCATargetsPath=c:\wix\SDK\wix.ca.targets /property:WixTargetsPath=c:\wix\wix.targets /property:WixTasksPath=c:\wix\WixTasks.dll /property:WixToolPath=c:\wix\  WixDataBase.Setup.sln  /t:rebuild 
REM msbuild.exe /property:WixSdkPath=c:\wix\sdk\ /property:WixTasksPath=c:\wix\WixTasks.dll /property:WixCATargetsPath=c:\wix\SDK\wix.ca.targets /property:WixTargetsPath=c:\wix\wix.targets /property:WixTasksPath=c:\wix\WixTasks.dll /property:WixToolPath=c:\wix\ WixDataBase.Setup.wixproj


