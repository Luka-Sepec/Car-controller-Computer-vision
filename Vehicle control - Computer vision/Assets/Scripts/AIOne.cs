using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
public class AIOne : MonoBehaviour //UFLD
{
    public Camera CVCam;
    public Car car;
    public RenderTexture rt;
    public Texture2D tex;
    public float laneCenter;
    private PredictData lastPredictData = null;
    public bool canSendFrame = true;
    [System.Serializable]
    public class LanePoint
    {
        public float x;
        public float y;
        public int laneIndex;

        public LanePoint(float[] arr)
        {
            x = arr[0];
            y = arr[1];
        }
    }

    [System.Serializable]
    public class Obstacle
    {
        public float x1;
        public float y1;
        public float x2;
        public float y2;
    }

    [System.Serializable]
    public class PredictData
    {
        public List<LanePoint> lanes;
        public List<Obstacle> obstacles;
    }
    private void Start()
    {
        rt = new RenderTexture(320, 240, 24);
        tex = new Texture2D(320, 240, TextureFormat.RGB24, false);
        CVCam.targetTexture = rt;

    }
    private void Update()
    {
        if (canSendFrame) StartCoroutine(SendFrame());
    }

    IEnumerator SendFrame()
    {
        canSendFrame = false;
        CVCam.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, 320, 240), 0, 0);
        tex.Apply();
        //CVCam.targetTexture = null;
        RenderTexture.active = null;

        byte[] bytes = tex.EncodeToPNG();
        UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:5000/predict", "POST");
        www.uploadHandler = new UploadHandlerRaw(bytes);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/octet-stream");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            var data = JsonUtility.FromJson<PredictData>(www.downloadHandler.text);
            //Debug.Log("Lanes received:");
            //foreach (var point in data.lanes)
            //{
            //    Debug.Log($"Lane {point.laneIndex}: x={point.x}, y={point.y}");
            //}
            //foreach (var obstacle in data.obstacles)
            //{
            //    Debug.Log($"Obstacle: {obstacle.x1}, {obstacle.y1}, {obstacle.x2}, {obstacle.y2}");
            //}
            lastPredictData = data;

            laneCenter = ComputeLaneCenter(data);
            //ApplyControls(laneCenter);
        }
        canSendFrame = true;
    }

    public float ComputeLaneCenter(PredictData data)
    {
        float totalX = 0f;
        int count = 0;
        float cameraWidth = 1280f;
        float screenWidth = 320f;
        
        foreach (var point in data.lanes)
        {
            float scaledX = point.x * screenWidth / cameraWidth;
            float scaledY = point.y / 3f;
            if (scaledY < 140f && scaledY > 20f)
            {
                totalX += scaledX;
                count++;
            }
        }
        float rawCenter = count > 0 ? totalX / count : screenWidth / 2f;
        return rawCenter;
    }
    public void ApplyControls(float laneX)
    {
        float screenCenter = 320f / 2f;
        float error = laneX - screenCenter;
        float steerValue = Mathf.Clamp(error / screenCenter * 2f, -1f, 1f);
        float targetSpeed = car.maxSpeed * (1 - steerValue);
        car.input.steerValue = steerValue;

        float speedFactor = 1f - Mathf.Abs(steerValue);

        //if (speedFactor > 0.9f)
        //    car.input.accelerationValue = 0.3f;
        //else if (speedFactor > 0.6f)
        //    car.input.accelerationValue = 0.1f;
        //else
        //    car.input.accelerationValue = -0.2f;
        if (car.currentSpeed > targetSpeed) car.input.accelerationValue = -0.4f;
        else if (car.currentSpeed < targetSpeed) car.input.accelerationValue = 0.4f;
        else car.input.accelerationValue = 0f;
    }

    void OnGUI()
    {
        if (tex != null)
        {
            int x = 10;
            int y = 10;
            int width = 320;
            int height = 240;

            // draw camera image
            GUI.DrawTexture(new Rect(x, y, width, height), tex);

            // draw lane center line
            float laneX = x + laneCenter;

            GUI.color = Color.red;
            GUI.DrawTexture(new Rect(laneX, y, 2, height), Texture2D.whiteTexture);
            GUI.color = Color.green;
            float cameraWidth = 1280f; // model output width
            float cameraHeight = 720f; // model output height

            if (lastPredictData != null)
            {
                foreach (var point in lastPredictData.lanes) 
                {
                    float scaledX = point.x * width / cameraWidth;
                    float scaledY = point.y * height / cameraHeight;
                    if (scaledY < 140f && scaledY > 20f)
                    {
                        GUI.color = Color.blue;
                    }
                    else
                    {
                        GUI.color = Color.green;
                    }
                    GUI.DrawTexture(new Rect(x + scaledX - 1, y + scaledY - 1, 3, 3), Texture2D.whiteTexture);
                }
                GUI.color = Color.white;

                foreach (var obstacle in lastPredictData.obstacles)
                {
                    float scaledX1 = obstacle.x1 * width / cameraWidth;
                    float scaledY1 = obstacle.y1 * height / cameraHeight;
                    float scaledX2 = obstacle.x2 * width / cameraWidth;
                    float scaledY2 = obstacle.y2 * height / cameraHeight;

                    float rectWidth = scaledX2 - scaledX1;
                    float rectHeight = scaledY2 - scaledY1;

                    GUI.color = Color.yellow;

                    // top
                    GUI.DrawTexture(
                        new Rect(x + scaledX1, y + scaledY1, rectWidth, 2),
                        Texture2D.whiteTexture
                    );

                    // bottom
                    GUI.DrawTexture(
                        new Rect(x + scaledX1, y + scaledY2, rectWidth, 2),
                        Texture2D.whiteTexture
                    );

                    // left
                    GUI.DrawTexture(
                        new Rect(x + scaledX1, y + scaledY1, 2, rectHeight),
                        Texture2D.whiteTexture
                    );

                    // right
                    GUI.DrawTexture(
                        new Rect(x + scaledX2, y + scaledY1, 2, rectHeight),
                        Texture2D.whiteTexture
                    );
                }
            }

        }
    }
}
