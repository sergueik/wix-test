@ECHO off

REM http://wix.sourceforge.net/manual-wix3/heat.htm

REM type sources: dir|file|perf|project|reg|website
REM -template fragment|module|product, -var var.SourceItemsDir Directorio de despliegue File/@Source = "$(var.SourceItemsDir)\MiFile.txt"
REM -pog grupo de salida para proyectos VS, por ejemplo: Binaries,Symbols,Documents,Satelites,Sources,Content. Puede ser repetido, por ejemplo: -pog Binaries -pog Documents -pog Satellites

REM Crea archivo de componentes usando directorio
REM "C:\Archivos de programa\Windows Installer XML v3.6\bin\heat.exe" dir "D:\Hugo\Proyectos\WixTutor\Script\Wix.DBScript" -cg CG_General -out Components_Dir.wxs -dr INSTALLLOCATION -gg -var var.SourceItemsDir -template product
"%WIX%bin\heat.exe" dir "D:\Hugo\Proyectos\WixTutor\Script\Wix.DBScript" -cg CG_General -out Components_Dir.wxs -dr INSTALLLOCATION -gg -var var.SourceItemsDir -template product

REM Crea archivo de componentes usando proyecto
REM "C:\Archivos de programa\Windows Installer XML v3.6\bin\heat.exe" project "D:\Hugo\Proyectos\WixTutor\Fuente\WixDataBase.Setup\WixDataBase.CustomAction\WixDataBase.CustomAction.csproj" -pog Binaries -pog Documents -pog Satellites -cg CG_General -out Components_Project.wxs -dr INSTALLLOCATION -gg -var var.SourceItemsDir -template product
"%WIX%bin\heat.exe" project "D:\Hugo\Proyectos\WixTutor\Fuente\WixDataBase.Setup\WixDataBase.CustomAction\WixDataBase.CustomAction.csproj" -pog Binaries -pog Documents -pog Satellites -cg CG_General -out Components_Project.wxs -dr INSTALLLOCATION -gg -var var.SourceItemsDir -template product

ECHO Presione una tecla para terminar ...
PAUSE
@ECHO on
