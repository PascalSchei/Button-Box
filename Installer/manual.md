# Installer bauen

Voraussetzung: NSIS ist installiert (`makensis.exe`, z.B. `C:\Program Files (x86)\NSIS`).

Alle Befehle im Ordner `Button-Box\Installer` ausführen.

## 1. Publish

Der Installer packt den Inhalt von `ButtonBoxBlazor\bin\Publish`. Dieser Ordner muss vorher frisch erzeugt werden:

```powershell
dotnet publish ..\ButtonBoxBlazor\ButtonBoxBlazor.csproj -c Release -p:PublishProfile=FolderProfile
```

Das Profil hat `DeleteExistingFiles=true`, alte Dateien im Zielordner werden dabei automatisch entfernt.

## 2. Installer kompilieren

```powershell
& "C:\Program Files (x86)\NSIS\makensis.exe" buttonBox.nsi
```

Ist `makensis` im `PATH`, genügt `makensis buttonBox.nsi`. Bei einem Pfad in Anführungszeichen braucht PowerShell den Aufrufoperator `&` davor.

Ergebnis: `ButtonBox-Setup-<VERSION>.exe` im selben Ordner. Die Version wird oben in `buttonBox.nsi` mit `!define VERSION` gesetzt.
