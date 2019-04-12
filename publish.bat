cd /d %~dp0
rd /Q /S bin\release 
rd /Q /S bin\lib 
dotnet publish -c release -r win-x64 --self-contained
rename bin\release\netcoreapp2.2\win-x64\publish lib
move bin\release\netcoreapp2.2\win-x64\lib bin\lib
copy postproc.bat bin\postproc.bat
copy app_template.bat.txt bin\EpubFootnoteAdapter.bat
rd /Q /S bin\release 
rd /Q /S bin\debug 
pause