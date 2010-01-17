@echo off

for /R ..\docs\ %%f in (*.html,*.htm) do  (
svn propset "svn:mime-type" "text/html" "%%f"

)

for /R ..\docs\ %%f in (*.gif) do  (
svn propset "svn:mime-type" "image/gif" "%%f"
)

for /R ..\docs\ %%f in (*.png) do  (
svn propset "svn:mime-type" "image/png" "%%f"
)

for /R ..\docs\ %%f in (*.js) do  (
svn propset "svn:mime-type" "text/javascript" "%%f"
)

for /R ..\docs\ %%f in (*.css) do  (
svn propset "svn:mime-type" "text/css" "%%f"
)

for /R ..\docs\ %%f in (*.bmp) do  (
svn propset "svn:mime-type" "image/bmp" "%%f"
)

for /R ..\docs\ %%f in (*.xml) do  (
svn propset "svn:mime-type" "text/xml" "%%f"
)
