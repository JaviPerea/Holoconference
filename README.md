# Project XYZ: Server-Receiver-Emisor Communication Setup

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
     python server.py
     ```
   - The server will start listening for connections.

### Emitter Setup and Execution
1. **Emitter Requirements:** Ensure you have Python installed on your system.
2. **Running the Emitter:**
   - Open a terminal.
   - Navigate to the emitter directory.
   - Execute the emitter script, specifying the connection to the server:
     ```
     python emitter.py --connect
     ```
   - Replace `--connect` with the appropriate connection details as needed.

### Receiver Setup and Execution
1. **Receiver Requirements:** Ensure you have the Godot Engine installed.
2. **Running the Receiver:**
   - Open the Godot Engine.
   - Load the project that contains the receiver scene.
   - Run the receiver scene from within the Godot editor.

## Notes
- **Execution Order:** Start with the server, followed by the emitter, and then the receiver.
- **Python Version:** Ensure Python 3.x is installed for running the server and emitter scripts.
- **Godot Setup:** Familiarize yourself with the Godot Engine to correctly load and execute the receiver scene.

## Troubleshooting
- If you encounter issues, check network configurations for server connectivity.
- Verify Python installations and script dependencies.
- Ensure the Godot project is correctly set up with necessary scene configurations.


