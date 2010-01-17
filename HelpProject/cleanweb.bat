@echo off
del Output\*.aspx
del Output\*.Config
rmdir /S /Q Output\fti
del Output\*.log
ren Output\Index.html index.html
pause