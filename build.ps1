$msbuild = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
$proj = "d:\documents\scl\ninth sem\dot net\dot net\kiro\CarSparePartsSystem\CarSparePartsSystem.csproj"
& $msbuild $proj /p:Configuration=Debug /nologo /verbosity:normal 2>&1
