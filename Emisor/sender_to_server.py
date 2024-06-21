import requests
import socket
import os
import json
import time


username = "Emisor"
ip_address = "127.0.0.1"  # IP address of the sender

port = 5066 
port_audio = 5067

selected_user =  None

session_socket = None

end_received = False



def register_user():
    """
    Register user on Flask server.
    """
    url = "http://127.0.0.1:5000/register"
    
    data = {
        'username': username,
        'ip_address': ip_address
    }

    try:
        response = requests.post(url, json=data)
        if response.status_code == 200:
            response_data = response.json()

            global port_session
            port_session = response_data.get('session_port')

            if port_session:
                print(f"User registered successfully. Session port: {port_session}")
            else:
                print("Error getting session port from server.")
        else:
            print(f"Error registering user. Status code: {response.status_code}")
    except Exception as e:
        print(f"Error registering user: {e}")


def request_other_users():
    """
    Request the list of registered usernames excluding the current user.
    """
    base_url = "http://127.0.0.1:5000"  
    current_user = "Emisor" 
    
    url = f"{base_url}/other_users?requesting_user={current_user}"

    try:
        response = requests.get(url)
        if response.status_code == 200:
            other_usernames = response.json()

            return other_usernames
        
        else:
            print(f"Failed to retrieve other registered usernames. Status code: {response.status_code}")
    except Exception as e:
        print(f"An error occurred: {e}")


def send_session_request():
    """
    Send session request to another user.
    """
    global selected_user
    
    url = "http://127.0.0.1:5000/request_session" 
    other_usernames = request_other_users()

    while not other_usernames:
        print("There are no registered users available. Waiting for someone to register...")
        time.sleep(5)  # Esperar 5 segundos antes de volver a consultar
        other_usernames = request_other_users()


    while True:
        for i, user in enumerate(other_usernames, 1):
            print(f"{i}. {user}")

        recipientIndex = input("Choose the user you want to call by dialing their number from the list: ")

        try:
            recipientIndex = int(recipientIndex) -1 
            if 0 <= recipientIndex < len(other_usernames):
                selected_user = other_usernames[recipientIndex]
                print("Calling to " + selected_user)
                break 
            else:
                print("Invalid index. Please select a valid user number: ")
        except ValueError:
            print("Invalid entry. Please enter a valid integer: ")

    
    data = {
      'sender': username,
      'recipient': selected_user,
      'data_port': port,
      'audio_port': port_audio
    }

    print("Request sent: ", data)
    
    try:
      response = requests.post(url, json=data)
      if response.status_code == 200:
         print("Session request sent successfully to the recipient.")
      else:
         print(f"Failed to send session request. Status code: {response.status_code}")
    except Exception as e:
      print(f"Error sending session request: {e}")

    receiver_address = wait_for_session_confirmation()

    return receiver_address




def wait_for_session_confirmation():
    """
    Wait for session confirmation.
    """
    global session_socket

    # Check if the socket has already been created
    if session_socket is None:
        session_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        session_socket.bind(('127.0.0.1', port_session))
    else:
        # If it is already created, the socket is closed and created again
        session_socket.close()
        session_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        session_socket.bind(('127.0.0.1', port_session))

    print("Waiting for session confirmation on port 8000...")

    data, receiver_address = session_socket.recvfrom(1024)
    receiver_address = receiver_address[0]

    print("Session answer received:")

    message = data.decode()
    print(message)

    if "denied" in message:
        # Raise an exception to signal that the session request was denied
        print("The session request has been rejected by the recipient.")
        send_session_request()

    return receiver_address


def receive_end():
    global session_socket
    global end_received
    
   # Check if the socket has already been created
    if session_socket is None:
        session_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        session_socket.bind(('127.0.0.1', port_session))
    else:
        # If it is already created, the socket is closed and created again
        session_socket.close()
        session_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        session_socket.bind(('127.0.0.1', port_session))

    print("Waiting for session end on port 8000...")

    data, receiver_address = session_socket.recvfrom(1024)
    receiver_address = receiver_address[0]

    print("End message received:")

    message = json.loads(data)

    # Check if it is a completion message
    if message.get('message_type') == 'end':
        end_received = True






def end_session():
    """
    End session with another user.
    """
    url = "http://127.0.0.1:5000/end_session"
    data = {
        'user' : username,
        'sender': username,
        'recipient': selected_user
    }

    try:
        response = requests.post(url, json=data)
        if response.status_code == 200:
            print("Session ended successfully.")
        else:
            print(f"Failed to end session. Status code: {response.status_code}")
    except Exception as e:
        print(f"Error ending session: {e}")



if __name__ == "__main__":
    register_user()
    send_session_request()
    end_session()