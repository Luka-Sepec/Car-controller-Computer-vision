from flask import Flask, request, jsonify
import cv2
import numpy as np
from ultralytics import YOLO

from Ufld.lane_detector import LaneDetector
app = Flask(__name__)

laneModel = LaneDetector("Ufld/models/tusimple_18.pth")
model = YOLO('yolov8n.pt')

@app.route('/predict', methods = ['POST'])

def predict():
    nparr = np.frombuffer(request.data, np.uint8)
    img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)

    lanes = laneModel.predict(img)
    formattedLanes = []
    for laneIndex, lane in enumerate(lanes):
        for p in lane:
            formattedLanes.append({"x": p[0], "y": p[1], "laneIndex": laneIndex})

    results = model.predict(img, imgsz = 320, verbose=False)
    obstacles = []
    if len(results) > 0 and len(results[0].boxes) > 0:
        for box in results[0].boxes.xyxy:
            x1, y1, x2, y2 = map(float, box[:4])
            obstacles.append({"x1": x1, "y1": y1, "x2": x2, "y2": y2})
    print(obstacles)
    
    return jsonify({
        "lanes": formattedLanes,
        "obstacles": obstacles
    })

if __name__ == '__main__':
    app.run(host = '0.0.0.0', port = 5000)