import cv2
import mediapipe as mp

class HandDetector:
    def __init__(self, static_image_mode=False, max_num_hands=2, min_detection_confidence=0.5, min_tracking_confidence=0.5):
        self.static_image_mode = static_image_mode
        self.max_num_hands = max_num_hands
        self.min_detection_confidence = min_detection_confidence
        self.min_tracking_confidence = min_tracking_confidence
        self.mp_hands = mp.solutions.hands
        self.hands = self.mp_hands.Hands(static_image_mode=self.static_image_mode,
                                          max_num_hands=self.max_num_hands,
                                          min_detection_confidence=self.min_detection_confidence,
                                          min_tracking_confidence=self.min_tracking_confidence)
        self.mp_draw = mp.solutions.drawing_utils

    def find_hands(self, img, draw=True):
        img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        self.results = self.hands.process(img_rgb)
        if self.results.multi_hand_landmarks:
            for hand_landmarks in self.results.multi_hand_landmarks:
                if draw:
                    self.mp_draw.draw_landmarks(img, hand_landmarks, self.mp_hands.HAND_CONNECTIONS)
        return img

    def process_hands(self, img):
        img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        self.results = self.hands.process(img_rgb)
        hands_data = []
        if self.results.multi_hand_landmarks:
            for hand_landmarks, handedness in zip(self.results.multi_hand_landmarks, self.results.multi_handedness):
                mano = {
                    "hand_landmarks": hand_landmarks,
                    "hand_label": handedness.classification[0].label,
                    "hand_index": handedness.classification[0].index
                    }
                hands_data.append(mano)
        return hands_data





def extract_wrist_positions(hand_data, img):
    wrist_positions_left = [0, 0]
    wrist_positions_right = [0, 0]
    img_height, img_width, _ = img.shape
    
    for mano in hand_data:
        landmarks = mano["hand_landmarks"]
        handedness = mano["hand_label"]
        
        print(handedness)
        
        wrist_landmark = landmarks.landmark[mp.solutions.hands.HandLandmark.WRIST]
        wrist_x = wrist_landmark.x
        wrist_y = wrist_landmark.y
        
        if handedness == "Left":
            wrist_positions_left = [wrist_x, wrist_y]
        elif handedness == "Right":
            wrist_positions_right = [wrist_x, wrist_y]
            
    return wrist_positions_left, wrist_positions_right



