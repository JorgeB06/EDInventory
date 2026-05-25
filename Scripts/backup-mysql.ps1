# ============================================================
#  backup-mysql.ps1
#  Backup automatico de la base de datos DB_EInventory
#  Uso: .\backup-mysql.ps1
#       Programar con Task Scheduler usando backup-mysql.bat
# ============================================================

# ── Configuracion ────────────────────────────────────────────
$DB_HOST    = "127.0.0.1"
$DB_PORT    = "3306"
$DB_NAME    = "DB_EInventory"
$DB_USER    = "root"
$DB_PASS    = "DevWork26#"

# Carpeta destino de backups (se crea si no existe)
$BACKUP_DIR = "D:\EDInventory\Backups"

# Cuantos backups conservar (los mas antiguos se eliminan automaticamente)
$KEEP_LAST  = 30

# Ruta a mysqldump.exe (ajustar si MySQL esta en otra ruta)
$MYSQLDUMP  = "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe"

# ── Verificaciones previas ───────────────────────────────────
if (-not (Test-Path $MYSQLDUMP)) {
    Write-Error "mysqldump.exe no encontrado en: $MYSQLDUMP"
    Write-Error "Ajuste la variable MYSQLDUMP en este script."
    exit 1
}

if (-not (Test-Path $BACKUP_DIR)) {
    New-Item -ItemType Directory -Path $BACKUP_DIR | Out-Null
    Write-Host "Carpeta de backups creada: $BACKUP_DIR"
}

# ── Generar nombre de archivo con timestamp ──────────────────
$timestamp  = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$fileName   = "DB_EInventory_$timestamp.sql"
$filePath   = Join-Path $BACKUP_DIR $fileName
$logFile    = Join-Path $BACKUP_DIR "backup.log"

# ── Ejecutar mysqldump ───────────────────────────────────────
Write-Host "Iniciando backup: $fileName"

$env:MYSQL_PWD = $DB_PASS   # evita warning de contrasena en consola

$args = @(
    "--host=$DB_HOST",
    "--port=$DB_PORT",
    "--user=$DB_USER",
    "--single-transaction",      # backup consistente sin bloquear tablas
    "--routines",                # incluye stored procedures
    "--triggers",                # incluye triggers
    "--add-drop-database",
    "--databases", $DB_NAME
)

& $MYSQLDUMP @args | Out-File -FilePath $filePath -Encoding utf8

$env:MYSQL_PWD = $null

if ($LASTEXITCODE -ne 0 -or (Get-Item $filePath).Length -lt 1000) {
    $msg = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] ERROR: backup fallido - $fileName"
    Add-Content -Path $logFile -Value $msg
    Write-Error $msg
    exit 1
}

# ── Log de exito ─────────────────────────────────────────────
$size = [math]::Round((Get-Item $filePath).Length / 1KB, 1)
$msg  = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] OK: $fileName ($size KB)"
Add-Content -Path $logFile -Value $msg
Write-Host $msg

# ── Rotacion: eliminar backups mas antiguos que $KEEP_LAST ───
$allBackups = Get-ChildItem -Path $BACKUP_DIR -Filter "DB_EInventory_*.sql" |
              Sort-Object LastWriteTime -Descending

if ($allBackups.Count -gt $KEEP_LAST) {
    $toDelete = $allBackups | Select-Object -Skip $KEEP_LAST
    foreach ($f in $toDelete) {
        Remove-Item $f.FullName -Force
        $delMsg = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] ELIMINADO (rotacion): $($f.Name)"
        Add-Content -Path $logFile -Value $delMsg
        Write-Host $delMsg
    }
}

Write-Host "Backup completado. Archivos conservados: $([math]::Min($allBackups.Count, $KEEP_LAST))"
