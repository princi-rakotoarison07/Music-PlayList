@echo off
chcp 65001 > nul
set PGPASSWORD=postgres
set DB_NAME=music-playlist
set DB_USER=postgres
set SQL_FILE=playlist-v0.sql

echo ========================================================
echo Reinitialisation de la base de donnees "%DB_NAME%"
echo ========================================================
echo.

:: Recherche automatique de psql.exe dans Program Files
set "PG_BIN="
for /d %%i in ("C:\Program Files\PostgreSQL\*") do (
    if exist "%%i\bin\psql.exe" (
        set "PG_BIN=%%i\bin"
    )
)

if "%PG_BIN%"=="" (
    echo [ERREUR] PostgreSQL n'a pas ete trouve dans C:\Program Files\PostgreSQL\
    echo Assurez-vous d'avoir installe PostgreSQL. Si le chemin est different,
    echo modifiez ce fichier .bat pour specifier le bon chemin.
    pause
    exit /b 1
)

echo [INFO] PostgreSQL trouve dans : %PG_BIN%
set PATH=%PG_BIN%;%PATH%
echo.

echo [1/4] Fermeture des connexions actives a "%DB_NAME%"...
psql -U %DB_USER% -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '%DB_NAME%' AND pid <> pg_backend_pid();"
if %errorlevel% neq 0 (
    echo [ERREUR] Impossible de se connecter a PostgreSQL.
    pause
    exit /b 1
)

echo [2/4] Suppression de la base de donnees "%DB_NAME%"...
psql -U %DB_USER% -d postgres -c "DROP DATABASE IF EXISTS \"%DB_NAME%\";"
if %errorlevel% neq 0 (
    echo [ERREUR] Impossible de supprimer la base de donnees.
    pause
    exit /b 1
)

echo [3/4] Creation de la nouvelle base de donnees "%DB_NAME%"...
psql -U %DB_USER% -d postgres -c "CREATE DATABASE \"%DB_NAME%\";"
if %errorlevel% neq 0 (
    echo [ERREUR] Impossible de creer la base de donnees.
    pause
    exit /b 1
)

echo [4/4] Importation du schema depuis "%SQL_FILE%"...
psql -U %DB_USER% -d %DB_NAME% -f "%~dp0%SQL_FILE%"
if %errorlevel% neq 0 (
    echo [ERREUR] Erreur lors de l'importation du script SQL.
    pause
    exit /b 1
)

echo.
echo ========================================================
echo Reinitialisation terminee avec succes !
echo ========================================================
pause
