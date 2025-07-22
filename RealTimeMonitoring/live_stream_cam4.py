import cv2
import datetime
import numpy as np
import requests
from ultralytics import YOLO
from flask import Flask, Response
from flask_cors import CORS
from requests.exceptions import Timeout

# INIT
app = Flask(__name__)
CORS(app)

# Paths
video_path = r"C:\Users\Sadik\Desktop\bitirme\Project\Records\uskudar_meydan.mp4"
yolo_model_path = "yolov8n.pt"
dashboard_url = "http://localhost:5001/Dashboard/Index/ReceiveTrafficData"
predict_api_url = "http://localhost:5006/predict"

# Location info
latitude = 41.0224
longitude = 29.0130
location_encoded = 1415
camera_id = 4

# Load YOLO
yolo = YOLO(yolo_model_path)
cap = cv2.VideoCapture(video_path)

def generate_frames():
    while True:
        ret, frame = cap.read()
        if not ret:
            cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
            continue
        print("Frame read:", ret)

        resized = cv2.resize(frame, (640, 360))

        # YOLO vehicle detection
        results = yolo.predict(source=resized, classes=[2, 3, 5, 7], conf=0.3, verbose=False)
        boxes = results[0].boxes
        vehicle_count = len(boxes)

        # Time features
        now = datetime.datetime.now()
        hour = now.hour
        day_of_week = now.weekday()
        is_weekend = 1 if day_of_week in [5, 6] else 0

        # Predict average speed via Flask
        try:
            prediction_response = requests.post(predict_api_url, json={
                "hour": hour,
                "dayOfWeek": day_of_week,
                "isWeekend": is_weekend,
                "latitude": latitude,
                "longitude": longitude,
                "numberOfVehicles": vehicle_count
            }, timeout=1.0)

            if prediction_response.status_code == 200:
                predicted_speed = prediction_response.json()["PredictedSpeed"]
                print(f"[Predicted] {predicted_speed:.2f} km/h")
            else:
                print(f"[Flask Error] {prediction_response.status_code}")
                predicted_speed = 0.0
        except Exception as e:
            print(f"[Prediction Failed] {e}")
            predicted_speed = 0.0

        # Annotate frame
        annotated = results[0].plot()
        cv2.putText(annotated, f'Vehicles: {vehicle_count}', (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
        cv2.putText(annotated, f'Speed: {predicted_speed:.1f} km/h', (10, 70), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (255, 0, 0), 2)

        # Send data to dashboard
        try:
            r = requests.post(dashboard_url, json={
                "Hour": hour,
                "DayOfWeek": day_of_week,
                "Latitude": latitude,
                "Longitude": longitude,
                "NumberOfVehicles": vehicle_count,
                "CameraId": camera_id,
                "PredictedSpeed": round(predicted_speed, 2)
            }, timeout=0.5)
            if r.status_code == 200:
                print("Dashboard update sent.")
            else:
                print(f"[Dashboard Error] {r.status_code}")
        except Timeout:
            print(f"[CAM {camera_id}] POST timeout.")
        except Exception as e:
            print(f"[CAM {camera_id}] Dashboard POST error: {e}")

        # Stream frame
        _, buffer = cv2.imencode('.jpg', annotated)
        frame_bytes = buffer.tobytes()
        yield (b'--frame\r\nContent-Type: image/jpeg\r\n\r\n' + frame_bytes + b'\r\n')


@app.route('/video_feed')
def video_feed():
    return Response(generate_frames(), mimetype='multipart/x-mixed-replace; boundary=frame')


if __name__ == '__main__':
    print("CAM 4 stream running on http://localhost:5005/video_feed")
    app.run(host='0.0.0.0', port=5005)
