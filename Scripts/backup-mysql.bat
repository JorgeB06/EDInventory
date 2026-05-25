@echo off
:: ============================================================
::  backup-mysql.bat
::  Wrapper para ejecutar backup-mysql.ps1 desde Task Scheduler
::
::  Configurar en Task Scheduler:
::    Programa:   C:\Windows\System32\cmd.exe
::    Argumentos: /c "D:\EDInventory\DiagramDB1.1\Scripts\backup-mysql.bat"
::    Inicio en:  D:\EDInventory\DiagramDB1.1\Scripts\
::
::  Frecuencia recomendada: diario a las 2:00 AM
:: ============================================================

PowerShell.exe -NonInteractive -ExecutionPolicy Bypass -File "%~dp0backup-mysql.ps1"
