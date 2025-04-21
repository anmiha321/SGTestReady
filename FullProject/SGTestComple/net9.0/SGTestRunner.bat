@echo off
echo SGTest
echo ===============================
echo 1. Import job titles
echo 2. Import departments
echo 3. Import employees
echo 4. Print full structure
echo 5. Exit
echo.

set /p option="Enter option (1-5): "

if "%option%"=="1" (
  SGTest.exe import --file=TSVData/jobtitle.tsv --type=jobtitle
) else if "%option%"=="2" (
  SGTest.exe import --file=TSVData/departments.tsv --type=department
) else if "%option%"=="3" (
  SGTest.exe import --file=TSVData/employees.tsv --type=employee
) else if "%option%"=="4" (
  SGTest.exe print
) else if "%option%"=="5" (
  exit
) else (
  echo Invalid option!
)

pause