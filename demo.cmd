@echo off

set profile=-p cmd
set x=bin\Debug\netcoreapp3.1

set a1=UpdateConfiguration
set a2=SampleProvider
set a3=WebAPI
set a4=SnapshotGenerator

wt              %profile% -d %a1%\%x% %a1%\%x%\%a1%.exe ; ^
  split-pane -V %profile% -d %a2%\%x% %a2%\%x%\%a2%.exe ; ^
  focus-tab  --target 0 ; ^
  split-pane -H %profile% -d %a3%\%x% %a3%\%x%\%a3%.exe ; ^
  split-pane    %profile% -d %a4%\%x% %a4%\%x%\%a4%.exe

REM This whole