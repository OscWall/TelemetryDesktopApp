# TelemetryDesktopApp

Desktopapplikation byggd i **C# med WinUI 3** för att visa sensordata från ESP32-enheter via ett REST API.

## Funktioner
- Visar upp till fyra ESP32-enheter.
- Temperatur och luftfuktighet per enhet.
- Genomsnittliga mätvärden.
- Enhetsstatus (Online/Offline).
- Senaste uppdateringstid.
- Sensorlista med aktuella mätvärden.
- Uppdateras automatiskt var 5:e sekund.

## Systemflöde

ESP32 → Raspberry Pi (MQTT) → C# Gateway → Azure IoT Hub → Python API → Desktop App

## Krav
- Windows 10/11
- Visual Studio 2022 med WinUI 3 / Windows App SDK
- .NET 8 SDK
- REST API som levererar sensordata

## Starta projektet

1. Ladda ner eller klona projektet.
2. Öppna `TelemetryDesktopApp.slnx` i Visual Studio.
3. Välj `TelemetryDesktopApp` som startprojekt.
4. Välj x64 och kör med **F5**.

## API

Desktopappen hämtar data från:

`http://127.0.0.1:8000/api/devices`

API-servern måste vara igång för att visa riktiga mätvärden.

Adressen kan ändras i `MainWindow.xaml.cs` om API:t körs på en annan dator.

## Teknik
- C#
- .NET 8
- WinUI 3
- Windows App SDK
- REST API / JSON
