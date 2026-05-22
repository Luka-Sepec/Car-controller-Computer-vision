from transformers import AutoImageProcessor, SegformerForSemanticSegmentation
from flask import Flask, request, jsonify
from PIL import Image
import numpy as np
import cv2
import torch

app = Flask(__name__)

processor = AutoImageProcessor.from_pretrained("nvidia/segformer-b0-finetuned-cityscapes-512-1024")
model = SegformerForSemanticSegmentation.from_pretrained("nvidia/segformer-b0-finetuned-cityscapes-512-1024")
model.eval()

@app.route('/predict', methods=['POST'])
def predict():
    nparr = np.frombuffer(request.data, np.uint8)
    imgcv = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    if imgcv is None:
        return jsonify({"error": "Invalid image"}), 400

    imgrgb = cv2.cvtColor(imgcv, cv2.COLOR_BGR2RGB)
    pilImg = Image.fromarray(imgrgb)

    inputs = processor(images=pilImg, return_tensors="pt")
    with torch.no_grad():
        outputs = model(**inputs)

    logits = outputs.logits
    upsampled = torch.nn.functional.interpolate(
        logits, size=(pilImg.height, pilImg.width), mode="bilinear", align_corners=False
    )
    segMask = torch.argmax(upsampled, dim=1)[0].cpu().numpy() 

    SMALL_H, SMALL_W = 32, 64
    smallMask = cv2.resize(segMask.astype(np.uint8), (SMALL_W, SMALL_H), interpolation=cv2.INTER_NEAREST)
    
    return jsonify({
        "segMask": smallMask.flatten().tolist(),
        "maskHeight": SMALL_H,
        "maskWidth": SMALL_W,
        "classes": ["road","sidewalk","building","wall","fence","pole","traffic light","traffic sign",
                    "vegetation","terrain","sky","person","rider","car","truck","bus","train",
                    "motorcycle","bicycle"]
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001)