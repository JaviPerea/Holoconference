from flask import Flask, request, jsonify
import socket
import json


app = Flask(__name__)
users = {}  # Dictionary to store registered users
sessions = {}  # Dictionary to store communication sessions

@app.route('/register', methods=['POST'])
def register_user():
    """
    Endpoint to register a user.
    """
    data = request.get_json()
    username = data.get('username')
    ip_address = data.get('ip_address')

    if username and ip_address:
        print(f"Nueva peticion de registro recibida para el usuario '{username}' desde la direccion IP '{ip_address}'.")
        if username in users:
            # Update the IP address of the existing user
            users[username] = {
                'ip_address': ip_address,
                'available': True  # User availability status
            }
            return jsonify({'message': 'Usuario existente. Direccion IP del usuario actualizada exitosamente','session_port': 8000}), 200
        else:
            # Add a new user to the list
            users[username] = {
                'ip_address': ip_address,
                'available': True 
            }
            return jsonify({'message': 'Usuario registrado exitosamente', 'session_port': 8000}), 200
    else:
        return jsonify({'error': 'Faltan datos en la solicitud'}), 400

    


@app.route('/other_users')
def get_other_registered_users():
    """
    Endpoint to obtain the usernames of the registered users, except the one making the request.
    """
    requesting_user = request.args.get('requesting_user')
    other_usernames = [username for username in users.keys() if username != requesting_user]
    return jsonify(other_usernames), 200



@app.route('/request_session', methods=['POST'])
def request_session():
    """
    Endpoint to send a session request to another user.
    """
    # Create a UDP socket to send the request to the receiver
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    data = request.get_json()
    sender = data.get('sender')  # User who makes the request
    recipient = data.get('recipient')  # User to whom the request is sent
    data_port = data.get('data_port')  # Sender data port
    audio_port = data.get('audio_port')  # Sender audio port

    # Check if the recipient is registered and available
    if recipient in users and users[recipient]['available']:
        # Send the request to the receiver
        recipient_ip = users[recipient]['ip_address']

        # Process the response message
        response = {
            'message': 'Solicitud de sesion realizada',
            'sender': sender,
            'message_type' : 'request', 
            'audio_port': audio_port,
            'data_port': data_port
        }

       # Send the message to the receiver
        sock.sendto(json.dumps(response).encode(), (recipient_ip, 8000))

        sessions[(sender, recipient)] = {
            'audio_port': audio_port,
            'data_port': data_port
        }


        # Information about the request is printed
        print(f"Solicitud de sesion de {sender} a {recipient}.")
        print(f"Puerto de datos del emisor: {data_port}")
        print(f"Puerto de audio del emisor: {audio_port}")

        return jsonify({'message': 'Solicitud de sesion enviada exitosamente'}), 200
        
    else:
        if (sender, recipient) in sessions:
            del sessions[(sender, recipient)]

        return jsonify({'error': 'El destinatario no esta disponible'}), 400


    

@app.route('/respond_session', methods=['POST'])
def respond_session():
    """
    Endpoint to respond to a session offer.
    """
    data = request.get_json()
    sender = data.get('sender')
    recipient = data.get('recipient')
    accepted = data.get('accepted')

    recipient_session = sessions.get((recipient, sender))

    if recipient_session:
        recipient_ip = users[recipient]['ip_address']
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

        if accepted:
            # Confirm the session and mark the users as busy
            users[sender]['available'] = False
            users[recipient]['available'] = False

            message = f"Solicitud de sesion aceptada"

            sock.sendto(message.encode(), (recipient_ip, 8000))

            return jsonify({'message': 'Sesion confirmada exitosamente'}), 200
        else:
            message = f"Solicitud de sesion denegada"

            sock.sendto(message.encode(), (recipient_ip, 8000))

            # Reject the session offer and remove it from the session dictionary
            del sessions[(recipient, sender)]

            return jsonify({'message': 'Oferta de sesion rechazada exitosamente'}), 200         

    else:
        return jsonify({'error': 'No se pudo encontrar la sesion'}), 400


@app.route('/end_session', methods=['POST'])
def end_session():
    """
    Endpoint to end a session.
    """
    data = request.get_json()
    user = data.get('user')
    sender = data.get('sender')
    recipient = data.get('recipient')

    destination_ip = None


    if user == sender :
        destination_ip = users[recipient]['ip_address']
    else:
        destination_ip = users[sender]['ip_address']
    
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    response = {
            'message': 'Fin de sesion',
            'message_type' : 'end'
        }
    

    # Send the message to the receiver
    sock.sendto(json.dumps(response).encode(), (destination_ip, 8000))


    if (sender, recipient) in sessions:
        # Mark users as available again
        users[sender]['available'] = True
        users[recipient]['available'] = True

        # Delete the session from the session dictionary
        del sessions[(sender, recipient)]

        print(f"Sesion finalizada de {sender} y {recipient}")
        return jsonify({'message': 'Sesion finalizada exitosamente'}), 200
    else:
        return jsonify({'error': 'No se encontro ninguna sesion asociada a los usuarios proporcionados'}), 400
    


if __name__ == '__main__':
    app.run(debug=True)




