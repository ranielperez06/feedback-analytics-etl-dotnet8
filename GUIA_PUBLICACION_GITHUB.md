# Publicación en GitHub

La entrega solicita el código fuente en GitHub. Sigue estos pasos después de
iniciar sesión en tu cuenta:

1. En GitHub, crea un repositorio vacío llamado
   `feedback-analytics-etl-dotnet8`.
2. No agregues README, `.gitignore` ni licencia desde GitHub; ya existen
   localmente.
3. Abre PowerShell en la carpeta `Actividad1_ETL_NET8`.
4. Ejecuta:

```powershell
git init
git branch -M main
git add .
git commit -m "feat: implement .NET 8 ETL extraction architecture"
git remote add origin https://github.com/TU-USUARIO/feedback-analytics-etl-dotnet8.git
git push -u origin main
```

## Qué hace cada comando

- `git init`: convierte la carpeta en un repositorio Git local.
- `git branch -M main`: establece `main` como rama principal.
- `git add .`: prepara todos los archivos permitidos por `.gitignore`.
- `git commit`: crea un punto de versión profesional y descriptivo.
- `git remote add origin`: conecta el repositorio local con GitHub.
- `git push`: publica el código en la cuenta del estudiante.

Antes de publicar, confirma que `.env`, contraseñas y archivos locales no estén
incluidos:

```powershell
git status
git ls-files
```

Nunca subas una cadena de conexión con una contraseña real.
