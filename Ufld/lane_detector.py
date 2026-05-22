import torch
import cv2
import numpy as np
from .ultrafastLaneDetector.ultrafastLaneDetector import UltrafastLaneDetector, ModelType  # adjust path if needed

class LaneDetector:
    def __init__(self, weights_path):
        self.model = UltrafastLaneDetector(weights_path, ModelType.TUSIMPLE, use_gpu=False)
      

    def predict(self, img):
        """
        img: RGB numpy array from OpenCV
        returns: list of lane keypoints [(x1,...), (x2,...), ...]
        """
        # resize and normalize like repo does
        self.model.detect_lanes(img, draw_points=False)
        return self.model.lanes_points