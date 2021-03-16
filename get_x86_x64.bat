DEL /F /Q C:\Users\mark\source\repos\findcnf3\findcnf_x64.exe
DEL /F /Q C:\Users\mark\source\repos\findcnf3\findcnf_x86.exe

COPY /Y C:\Users\mark\source\repos\findcnf\obj\x64\Release\findcnf3.exe C:\Users\mark\source\repos\findcnf\findcnf_x64.exe
COPY /Y C:\Users\mark\source\repos\findcnf\obj\x86\Release\findcnf3.exe C:\Users\mark\source\repos\findcnf\findcnf_x86.exe
