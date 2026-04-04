@echo off
set UNITY_EXE="C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe"
set PROJECT_PATH="C:\Users\gram\Desktop\timedevil\timedevil"

%UNITY_EXE% -projectPath %PROJECT_PATH% -diag-job-temp-memory-leak-validation
pause