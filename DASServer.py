from transformers import AutoImageProcessor, SegformerForSemanticSegmentation
from flask import Flask, request, jsonify
from PIL import Image
import numpy as np
import cv2
import torch
import torch.nn.functional as F

app = Flask(__name__)

processor = AutoImageProcessor.from_pretrained(
    "samuelsze/segformer-b0-drivable-area_segmentation")
model = SegformerForSemanticSegmentation.from_pretrained(
    "samuelsze/segformer-b0-drivable-area_segmentation")
model.eval()

@app.route('/predict', methods=['POST'])
def predict():
    nparr = np.frombuffer(request.data, np.uint8)
    imgcv = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    if imgcv is None:
        return jsonify({"error": "Invalid image data"}), 400

    imgrgb = cv2.cvtColor(imgcv, cv2.COLOR_BGR2RGB)
    pilImage = Image.fromarray(imgrgb)

    inputs = processor(images=pilImage, return_tensors="pt")
    with torch.no_grad():
        outputs = model(**inputs)
    logits = outputs.logits  

    upsampledLogits = F.interpolate(
        logits, size=(pilImage.height, pilImage.width),
        mode="bilinear", align_corners=False
    )
    predictedMask = torch.argmax(upsampledLogits, dim=1)[0].cpu().numpy()  

    drivableMask = (predictedMask == 1).astype(np.uint8)

    SMALL_H, SMALL_W = 32, 64
    smallMask = cv2.resize(
        drivableMask,
        (SMALL_W, SMALL_H),
        interpolation=cv2.INTER_NEAREST
    )

    return jsonify({
        "drivableMask": smallMask.flatten().tolist(),  
        "maskHeight": SMALL_H,
        "maskWidth": SMALL_W
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)