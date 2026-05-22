from flask import Flask, request, jsonify
import cv2
import numpy as np
import torch
from DepthAnythingV2.depth_anything_v2.dpt import DepthAnythingV2

app = Flask(__name__)

model = DepthAnythingV2(encoder='vits', features=64, out_channels=[48, 96, 192, 384])
model.load_state_dict(torch.load('checkpoints/depth_anything_v2_vits.pth', map_location='cpu')) 
model.eval()

@app.route('/predict', methods=['POST'])
def predict():
    nparr = np.frombuffer(request.data, np.uint8)
    imgcv = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    if imgcv is None:
        return jsonify({"error": "Invalid image"}), 400

    depthMap = model.infer_image(imgcv)      

    depthNorm = cv2.normalize(depthMap, None, 0, 255, cv2.NORM_MINMAX)
    depthNorm = depthNorm.astype(np.uint8)

    SMALL_H, SMALL_W = 32, 64
    smallDepth = cv2.resize(depthNorm, (SMALL_W, SMALL_H), interpolation=cv2.INTER_NEAREST)
    return jsonify({
        "depthMap": smallDepth.flatten().tolist(),
        "maskHeight": SMALL_H,
        "maskWidth": SMALL_W
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)