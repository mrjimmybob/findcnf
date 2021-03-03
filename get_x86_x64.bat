DEL /F /Q C:\Users\mark\source\repos\findcnf3\findcnf3_x64.exe
DEL /F /Q C:\Users\mark\source\repos\findcnf3\findcnf3_x86.exe

COPY /Y C:\Users\mark\source\repos\findcnf3\obj\x64\Release\findcnf3.exe C:\Users\mark\source\repos\findcnf3\findcnf3_x64.exe

COPY /Y C:\Users\mark\source\repos\findcnf3\obj\x86\Release\findcnf3.exe C:\Users\mark\source\repos\findcnf3\findcnf3_x86.exe
