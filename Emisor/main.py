"""
Main program to run the detection and UDP
"""

from argparse import ArgumentParser
import cv2
import mediapipe as mp
import numpy as np
import socket
from facial_landmark import FaceMeshDetector
from pose_estimator import PoseEstimator
from stabilizer import Stabilizer
from facial_features import FacialFeatures, Eyes
from hand_detector import HandDetector, extract_wrist_positions
import sys
import json

import pyaudio
import soundfile as sf
import os
import threading
import struct
import time
import wave

from pyogg import OpusEncoder

import sender_to_server

import atexit

# global variable
username = "Emisor"
ip_address = "127.0.0.1"  # Dirección IP del emisor

receiver_address = 0

port = 5066 
port_audio=5067
port_session=0

RATE= 16000
CHANNELS = 1
FORMAT = pyaudio.paInt16
CHUNKS = 960


start_time = time.time()


def init_UDP(receiver_address):
    """
    Initialize UDP connection with Godot.
    """
    tuple = (receiver_address, port)
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

        return s, tuple
    except OSError as e:
        print("Error while initializing UDP socket:", e)
        sys.exit()


def send_audio(receiver_address):

    global stop_threads

    address_audio = (receiver_address, port_audio)

    # Configure the socket for RTP
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    # Sequence and timestamp are initialized
    seq_number = 0
    timestamp = 0

    # PyAudio is configured to capture audio from the microphone
    audio = pyaudio.PyAudio()
    stream = audio.open(format=FORMAT,channels=CHANNELS,rate=RATE,input=True,frames_per_buffer=CHUNKS)


    bytes_encoded = 0

    # Create an Opus encoder
    opus_encoder = OpusEncoder()
    opus_encoder.set_application("audio")
    opus_encoder.set_sampling_frequency(RATE)
    opus_encoder.set_channels(CHANNELS)


    # Loop through the wav file's PCM data and encode it as Opus
    bytes_encoded = 0
  
    while True:
        # Audio data is read from the microphone
        pcm = stream.read(CHUNKS)
        

        # Check if we've finished reading the wav file
        if len(pcm) == 0:
            break

                
        # Encode the PCM data
        encoded_packet = opus_encoder.encode(pcm)
        bytes_encoded += len(encoded_packet)


        # RTP header is built
        version = 2  # RTP versión 2
        padding = 0  # No padding
        extension = 0  # No extension
        csrc_count = 0  # No CSRC identifiers
        marker = 0  # No specific payload information
        payload_type = 111  # Payload type according to your codec
        ssrc = 12345  # Unique identifier for the sender
            
        # RTP header is packaged
        rtp_header = struct.pack('!BBHII', 
                                (version << 6) | (padding << 5) | (extension << 4) | csrc_count,
                                (marker << 7) | payload_type,
                                seq_number,
                                timestamp,
                                ssrc)
    

        # Opus audio is packaged with RTP header
        payload = rtp_header + encoded_packet
            
        # Audio data is sent through the UDP socket
        sock.sendto(payload, address_audio)

        print("Sending audio")

            
        # Sequence number is increased
        seq_number += 1
            
        # Timestamp is updated
        current_time = time.time() - start_time
        timestamp = int(current_time * 1000)
            
        # Wait a little to maintain the shipping rate
        time.sleep(0.02)


def send_info(sock, address, args):
    """
    Send information to Godot over RTP connection in JSON format.
    """
    
    # Each numerical value of the parameters is rounded to four decimal places
    rounded_args = [round(arg, 4) if isinstance(arg, float) else arg for arg in args]

    data = {
        "roll": rounded_args[0],
        "pitch": rounded_args[1],
        "yaw": rounded_args[2],
        "ear_left": rounded_args[3],
        "ear_right": rounded_args[4],
        "x_ratio_left": rounded_args[5],
        "y_ratio_left": rounded_args[6],
        "x_ratio_right": rounded_args[7],
        "y_ratio_right": rounded_args[8],
        "mar": rounded_args[9],
        "mouth_distance": rounded_args[10],
        "left_hand_x": rounded_args[11],
        "left_hand_y": rounded_args[12],
        "right_hand_x": rounded_args[13],
        "right_hand_y": rounded_args[14]
    }

    json_data = json.dumps(data)

    # Sequence and timestamp are initialized
    seq_number = 0
    timestamp = 0

    # RTP header is built
    version = 2  # RTP versión 2
    padding = 0  # No padding
    extension = 0  # No extension
    csrc_count = 0  # No CSRC identifiers
    marker = 0  # No specific payload information
    payload_type = 0  # Payload type according to your codec
    ssrc = 12345  # Unique identifier for the sender

    try:         
        # RTP header is packaged
        rtp_header = struct.pack('!BBHII', 
                                (version << 6) | (padding << 5) | (extension << 4) | csrc_count,
                                (marker << 7) | payload_type,
                                seq_number,
                                timestamp,
                                ssrc)
                
        # Opus audio is packaged with RTP header
        payload = rtp_header + json_data.encode('utf-8')
           
        # Audio data is sent through the UDP socket
        sock.sendto(payload, address)
                
        # Sequence number is increased
        seq_number += 1
                
        # Timestamp is updated
        current_time = time.time() - start_time
        timestamp = int(current_time * 1000)

    except Exception as e:
        print("Error while sending JSON data:", e)
        sys.exit()

def print_debug_msg(args):
    """
    Print debug message.
    """
    msg = '%.4f ' * len(args) % args
    print(msg)


def main():
    """
    Main function to run the detection and UDP communication with Godot.
    """

    # The user is registered on the server
    sender_to_server.register_user()

    # A session request is made
    receiver_address = sender_to_server.send_session_request()

    # Listen for end request in a separate thread
    receive_end_thread = threading.Thread(target=sender_to_server.receive_end)
    receive_end_thread.daemon = True
    receive_end_thread.start()


    # Use internal webcam/USB camera
    cap = cv2.VideoCapture(args.cam)

    # FaceMesh detector
    detector = FaceMeshDetector()

    # Hand detector
    hand_detector = HandDetector()

    # Get a sample frame for pose estimation image
    success, img = cap.read()

    # Pose estimation related
    pose_estimator = PoseEstimator((img.shape[0], img.shape[1]))
    image_points = np.zeros((pose_estimator.model_points_full.shape[0], 2))
    iris_image_points = np.zeros((10, 2))

    # Initialize scalar stabilizers for pose
    pose_stabilizers = [Stabilizer(
        state_num=2,
        measure_num=1,
        cov_process=0.1,
        cov_measure=0.1) for _ in range(6)]
    

    # Initialize TCP connection if required
    if args.connect:
        socket, receiver_tuple = init_UDP(receiver_address)
        # Iniciar el envío de audio
        # udp_socket, address_audio = init_audio()
        send_audio_thread = threading.Thread(target=send_audio, args=(receiver_address,))
        send_audio_thread.daemon = True
        send_audio_thread.start()

    while cap.isOpened():

        if sender_to_server.end_received:
            break
            
        success, img = cap.read()
        if not success:
            print("Ignoring empty camera frame.")
            continue

        # Face detection and landmark detection
        img_facemesh, faces = detector.findFaceMesh(img)

        # Flip the input image
        img = cv2.flip(img, 1)

        # If there is any face detected
        if faces:
            # Extract facial landmarks and perform pose estimation
            for i in range(len(image_points)):
                image_points[i, 0] = faces[0][i][0]
                image_points[i, 1] = faces[0][i][1]

            for j in range(len(iris_image_points)):
                iris_image_points[j, 0] = faces[0][j + 468][0]
                iris_image_points[j, 1] = faces[0][j + 468][1]

            pose = pose_estimator.solve_pose_by_all_points(image_points)
            x_ratio_left, y_ratio_left = FacialFeatures.detect_iris(image_points, iris_image_points, Eyes.LEFT)
            x_ratio_right, y_ratio_right = FacialFeatures.detect_iris(image_points, iris_image_points, Eyes.RIGHT)
            ear_left = FacialFeatures.eye_aspect_ratio(image_points, Eyes.LEFT)
            ear_right = FacialFeatures.eye_aspect_ratio(image_points, Eyes.RIGHT)
            pose_eye = [ear_left, ear_right, x_ratio_left, y_ratio_left, x_ratio_right, y_ratio_right]
            mar = FacialFeatures.mouth_aspect_ratio(image_points)
            mouth_distance = FacialFeatures.mouth_distance(image_points)
            steady_pose = []
            pose_np = np.array(pose).flatten()

            for value, ps_stb in zip(pose_np, pose_stabilizers):
                ps_stb.update([value])
                steady_pose.append(ps_stb.state[0])

            steady_pose = np.reshape(steady_pose, (-1, 3))

            roll = np.clip(np.degrees(steady_pose[0][1]), -90, 90)
            pitch = np.clip(-(180 + np.degrees(steady_pose[0][0])), -90, 90)
            yaw = np.clip(np.degrees(steady_pose[0][2]), -90, 90)
            
            # Detect hands in the image
            img_hands = hand_detector.find_hands(img)

            # Process hands to obtain landmarks
            hand_landmarks = hand_detector.process_hands(img_hands)
            
            left_hand_x, left_hand_y, right_hand_x, right_hand_y = 0, 0, 0, 0 

            if hand_landmarks:
                # Extract wrist positions for each hand
                wrist_positions_left, wrist_positions_right = extract_wrist_positions(hand_landmarks, img)
                       
                left_hand_x = wrist_positions_left[0]
                left_hand_y = wrist_positions_left[1]
                right_hand_x = wrist_positions_right[0]
                right_hand_y = wrist_positions_right[1]
                
                
            # Send information to Godot
            if args.connect:
                send_info(socket, receiver_tuple,
                        (roll, pitch, yaw,
                        ear_left, ear_right, x_ratio_left, y_ratio_left, x_ratio_right, y_ratio_right,
                        mar, mouth_distance, left_hand_x, left_hand_y, right_hand_x, right_hand_y))

            # Print the sent values in the terminal
            if args.debug:
                print_debug_msg((roll, pitch, yaw,
                                 ear_left, ear_right, x_ratio_left, y_ratio_left, x_ratio_right, y_ratio_right,
                                 mar, mouth_distance, left_hand_x, left_hand_y, right_hand_x, right_hand_y))

        # Display facial landmark detection
        cv2.imshow('Facial landmark', img_facemesh)

        if cv2.waitKey(1) & 0xFF == ord('q'):
            sender_to_server.end_session()
            break


    # Release the camera
    cap.release()

if __name__ == "__main__":
    parser = ArgumentParser()
    parser.add_argument("--connect", action="store_true",
                        help="connect to Godot character",
                        default=False)
    parser.add_argument("--cam", type=int,
                        help="specify the camera number if you have multiple cameras",
                        default=0)
    parser.add_argument("--debug", action="store_true",
                        help="show raw values of detection in the terminal",
                        default=False)

    args = parser.parse_args()

    # Run the main function
    main()

