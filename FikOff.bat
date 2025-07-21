@echo off
setlocal EnableDelayedExpansion
:: this took me so fucking long to make.
pushd "%~dp0" 
:: entry point: decides between interactive or command-line mode
if "%~1"=="" (
    call :interactive_mode
) else (
    call :command_line_mode %*
)

echo.
echo FikOff.bat has finished.
popd
goto :eof

:: interactive mode
:interactive_mode
    cls
    echo    GAME MODES:
    echo    SP: Singleplayer (Uninstalls FIKA)
    echo    MPH: Multiplayer Host (Installs FIKA)
    echo    MPC: Multiplayer Client (Installs FIKA)
    echo    QUICK MODES (No file/IP setup):
    echo    QSP, QMPH, QMPC
    echo.
    set /p "choice=Gonna squad up, go solo, or just wanna launch the damn game? (Q to Quit): "
    if /I "%choice%"=="Q" goto :eof

    :: i found this on stackoverflow
    for %%i in (A B C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
        set "choice=!choice:%%i=%%i!"
    )

    set "quick_mode=false"
    if /I "%choice:~0,1%"=="Q" (
        set "quick_mode=true"
        set "game_mode=%choice:~1%"
    ) else (
        set "game_mode=%choice%"
    )

    if /I "!game_mode!"=="SP" (
        if "!quick_mode!"=="false" (
            call :setup_sp
            call :set_ip 1
        )
        call :ask_to_start sl
    ) else if /I "!game_mode!"=="MPH" (
        if "!quick_mode!"=="false" (
            call :setup_mp
            call :ask_for_mp_ip
        )
        call :ask_to_start sl
    ) else if /I "!game_mode!"=="MPC" (
        if "!quick_mode!"=="false" (
            call :setup_mp
            call :ask_for_mp_ip
        )
        call :ask_to_start l
    ) else (
        echo.
        echo You managed to FikOff^(TM^) CLI input. Incredible.
    )
pause
goto :eof

:: CLAP
:: example: FikOff.bat /mph 2 /setup (starts host, uses line 2 of ip.txt, does setup)
:command_line_mode
    echo Running in command-line mode...
    
    :: these are the flags for each arg
    set "mode="
    set "ip_index="
    set "quick_flag=false"
    set "do_setup_flag=false"

    :: loops through all arguments to find the modes
    for %%a in (%*) do (
        if /I "%%a"=="/sp" set "mode=sp"
        if /I "%%a"=="/mph" set "mode=mph"
        if /I "%%a"=="/mpc" set "mode=mpc"
        if /I "%%a"=="/quick" set "quick_flag=true"
        if /I "%%a"=="/setup" set "do_setup_flag=true"
    )

    :: this mf is the one that forces you to set arguments in order
    if /I "%~1"=="/mph" if not "%~2"=="" if not "%~2"=="/quick" if not "%~2"=="/setup" set "ip_index=%~2"
    if /I "%~1"=="/mpc" if not "%~2"=="" if not "%~2"=="/quick" if not "%~2"=="/setup" set "ip_index=%~2"
    
    :: defaults to line 2 if no index is provided
    if not "!mode!"=="sp" if "!ip_index!"=="" set "ip_index=2"

    :: exits if user fucked up
    if "!mode!"=="" (
        echo Invalid mode argument provided. Use /sp, /mph, or /mpc.
        goto :eof
    )

    :: runs unless /quick is provided
    if /I "%quick_flag%"=="false" (
        if /I "!mode!"=="sp" (
            call :set_ip 1
        ) else (
            call :set_ip !ip_index!
        )
    ) else (
        echo [/quick flag detected] Skipping IP change...
    )

    :: runs only if /setup is present and not cock-blocked by /quick
    if /I "%do_setup_flag%"=="true" (
        if /I "%quick_flag%"=="false" (
            echo [/setup flag detected] Executing setup...
            if /I "!mode!"=="sp" (
                call :setup_sp
            ) else (
                call :setup_mp
            )
        ) else (
            echo [/setup and /quick flags detected] Skipping setup...
        )
    )

    :: this always runs
    echo.
    echo [Executing Start Block]
    if /I "!mode!"=="sp" (
        call :start_processes sl
    ) else if /I "!mode!"=="mph" (
        call :start_processes sl
    ) else if /I "!mode!"=="mpc" (
        call :start_processes l
    )

goto :eof

:ask_to_start
    echo.
    set /p "start_choice=Do you want to configure the launcher and start it automatically? (Y/N/Q to Quit): "
    if /I "%start_choice%"=="Q" goto :eof
    if /I "%start_choice%"=="Y" (
        call :start_processes %1
    )
goto :eof

:ask_for_mp_ip
    echo.
    echo There are multiple IPs in the list. Which one do you want to connect to?
    set /a line_num=0
    for /f "usebackq skip=1 delims=" %%A in ("ip.txt") do (
        set /a line_num+=1
        set /a display_num=line_num+1
        echo Line !display_num! - %%A
        timeout /t 1 >nul
    )
    echo.
    set /p "ip_line=Enter the number of the IP you want to use (Q to Quit): "
    if /I "%ip_line%"=="Q" goto :eof
    if "%ip_line%" NEQ "" call :set_ip %ip_line%
goto :eof

:setup_mp
    echo.
    echo ~~~ DELETING FILES TO AVOID ERRORS ~~~
    call :setup_sp_silent
    echo Residues removed!
    echo.
    echo ~~~ INSTALLING FIKA ~~~
    xcopy /E /I /Y /Q "_fika\BepInEx\plugins" "BepInEx\plugins" >nul
    xcopy /E /I /Y /Q "_fika\user\mods" "user\mods" >nul
    echo FIKA installed
    echo.
goto :eof

:setup_sp
    echo.
    echo ~~~ UNINSTALLING FIKA ~~~
    if exist "BepInEx\plugins\Fika.Core.dll" (
        del /F /Q "BepInEx\plugins\Fika.Core.dll" >nul
    )
    if exist "user\mods\fika-server" (
        rmdir /S /Q "user\mods\fika-server" >nul
    )
    echo FIKA successfully uninstalled
    echo.
goto :eof

:setup_sp_silent
    if exist "BepInEx\plugins\Fika.Core.dll" (
        del /F /Q "BepInEx\plugins\Fika.Core.dll" >nul
    )
    if exist "user\mods\fika-server" (
        rmdir /S /Q "user\mods\fika-server" >nul
    )
goto :eof

:: sets the IP in config.json AND ENABLES DEV MODE
:set_ip
    echo.
    echo ~~~ CHANGING LAUNCHER IP ~~~
    set "ipFile=ip.txt"
    set "ip="
    set /A count=0
    set "lineToRead=%1"

    if not exist "%ipFile%" (
        echo The IP file doesn't exist. Can't launch in MP mode.
        goto :eof
    )

    for /F "usebackq delims=" %%A in ("%ipFile%") do (
        set /A count+=1
        if !count! EQU %lineToRead% (
            set "ip=%%A"
            goto :gotIP
        )
    )

    :gotIP
    if "!ip!"=="" (
        echo ERROR: IP not found at line %lineToRead% in %ipFile%.
        goto :eof
    )
    
    set "ip_display_num=%1"
    set "ip_address=!ip!"
    echo IP !ip_display_num! selected! Address: !ip_address!

    set "jsonPath=user\launcher\config.json"
    set "key_url=\"Url\""
    set "key_devmode=\"IsDevMode\""
    set "newval_ip="!ip!""

    if not exist "%jsonPath%" (
        set "path_val=%cd%\%jsonPath%"
        echo Launcher config file not found. Make sure it's located at !path_val!.
        goto :eof
    )

    > "%jsonPath%.tmp" (
      for /f "usebackq delims=" %%L in ("%jsonPath%") do (
        set "line=%%L"
        echo !line! | findstr /C:%key_url% >nul
        if not errorlevel 1 (
            for /f "tokens=1* delims=:" %%a in ("!line!") do (
                set "indent=%%a"
                echo !indent!: !newval_ip!,
            )
        ) else (
            echo !line! | findstr /C:%key_devmode% >nul
            if not errorlevel 1 (
                for /f "tokens=1* delims=:" %%a in ("!line!") do (
                    set "indent=%%a"
                    echo !indent!: true,
                )
            ) else (
                echo !line!
            )
        )
      )
    )
    move /Y "%jsonPath%.tmp" "%jsonPath%" >nul
    echo Launcher config updated. Dev Mode enabled.
goto :eof

:start_processes
    set "arg=%1"
    echo.
    if /I "%arg%"=="sl" (
        echo ~~~ STARTING SERVER ~~~
        start "" "SPT.Server.exe"
        echo Waiting 10 seconds for the server to spin up...
        timeout /t 10 /nobreak >nul
        echo ~~~ STARTING LAUNCHER ~~~
        start "" "SPT.Launcher.exe"
    ) else if /I "%arg%"=="l" (
        echo ~~~ STARTING LAUNCHER ~~~
        start "" "SPT.Launcher.exe"
    ) else (
        echo.
        echo INTERNAL ERROR: Invalid argument for start function.
    )
goto :eof