DEL /F /Q findcnf_x64.exe
DEL /F /Q findcnf_x86.exe

COPY /Y bin\x64\Release\findcnf3.exe findcnf_x64.exe
COPY /Y bin\x86\Release\findcnf3.exe findcnf_x86.exe
