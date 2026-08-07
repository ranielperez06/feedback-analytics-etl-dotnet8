# Publicación en GitHub

La entrega solicita el código fuente en GitHub. Sigue estos pasos después de
iniciar sesión en tu cuenta:

El repositorio ya existe:

https://github.com/ranielperez06/feedback-analytics-etl-dotnet8

Para publicar la continuación de carga dimensional, abre PowerShell en la
carpeta `Actividad1_ETL_NET8` y ejecuta:

```powershell
git add .
git commit -m "feat: load data warehouse dimensions"
git push origin main
```

## Qué hace cada comando

- `git add .`: prepara todos los archivos permitidos por `.gitignore`.
- `git commit`: crea un punto de versión profesional y descriptivo.
- `git push`: publica el código en la cuenta del estudiante.

Antes de publicar, confirma que `.env`, contraseñas y archivos locales no estén
incluidos:

```powershell
git status
git ls-files
```

Nunca subas una cadena de conexión con una contraseña real.
