# Project Holoconference: Server-Receiver-Sender Communication Setup

## Overview
This project demonstrates communication between a server, a receiver, and an emitter. Follow these steps to set up and run each component in sequence.

## Instructions

### Server Setup and Execution
1. **Server Requirements:** Ensure you have Python installed on your system.
2. **Running the Server:**
   - Open a terminal.
   - Navigate to the server directory.
   - Execute the server script using Python:
     ```
     python log_server.py
     ```
   - The server will start listening for connections.

### Receiver Setup and Execution
1. **Receiver Requirements:** Ensure you have the Godot Engine installed.
2. **Running the Receiver:**
   - Open the Godot Engine.
   - Load the project that contains the receiver scene.
   - Run the receiver scene "EscenaComunicacion.tscn" from within the Godot editor.
  
### Sender Setup and Execution
1. **Sender Requirements:** Ensure you have Python installed on your system.
2. **Running the Sender:**
   - Open a terminal.
   - Navigate to the emitter directory.
   - Execute the emitter script, specifying the connection to the server:
     ```
     python main.py --connect
     ```

## Notes
- **Execution Order:** Start with the server, followed by the receiver, and then the sender.
- **Python Version:** Ensure Python 3.x is installed for running the server and emitter scripts.
- **Godot Setup:** Familiarize yourself with the Godot Engine to correctly load and execute the receiver scene.
- **Required Libraries:** Make sure you have the following libraries installed:
  - Flask
  - Mediapipe
  - PyOgg
  - threading
  - cv2
  - numpy
  - struct
  - time
  - pyaudio
  - json
  - Concentus (in server directory)

## Troubleshooting
- If you encounter issues, check network configurations for server connectivity.
- Verify Python installations and script dependencies.
- Ensure the Godot project is correctly set up with necessary scene configurations.


