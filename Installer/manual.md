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

## Unterschied zwischen `buttonBox.nsi` und `buttonBoxTest.nsi`

Beide Skripte sind bis auf zwei Punkte identisch: den Namen der erzeugten Setup-Datei und den Umgang mit Fehlern beim Download oder bei der Installation der .NET-Runtimes.

| | `buttonBox.nsi` | `buttonBoxTest.nsi` |
|---|---|---|
| Ausgabedatei | `ButtonBox-Setup-<VERSION>.exe` | `ButtonBox-Setup-Test-<VERSION>.exe` |
| Fehler beim Runtime-Download | Fehlermeldung (`MB_ICONSTOP`), danach `Abort`: die Installation bricht ab | Warnung (`MB_ICONEXCLAMATION`) mit Hinweis auf manuelle Installation, die Installation läuft weiter |
| Fehler bei der Runtime-Installation (Exit-Code ungleich 0 und 3010) | Fehlermeldung, danach `Abort` | Warnung mit Hinweis auf manuelle Installation, die Installation läuft weiter |

`buttonBox.nsi` ist der Release-Installer. `buttonBoxTest.nsi` dient zum Testen auf Rechnern ohne Internetzugang oder mit blockierten Downloads: Die Runtimes werden bei Fehlern nur gemeldet, die Anwendung wird trotzdem installiert. Beim Erhöhen der Version muss `!define VERSION` in beiden Dateien angepasst werden.

```powershell
& "C:\Program Files (x86)\NSIS\makensis.exe" buttonBoxTest.nsi
```
