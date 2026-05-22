using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
public class AIFour : MonoBehaviour //SegFormer CityScapes
{
    public Camera CVCam;
    public Car car;
    public RenderTexture rt;
    public Texture2D tex;
    public bool canSendFrame = true;
    public PredictData lastPredictData = null;

    [System.Serializable]
    public class PredictData
    {
        public List<int> segMask;
        public int maskHeight;
        public int maskWidth;
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
        UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:5001/predict", "POST");
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
            lastPredictData = data;
            float steerValue = ComputeSteering();
            ApplyControls(steerValue);
            //Debug.Log(data.seg_mask);

        }
        canSendFrame = true;
    }

    public int GetMaskValue(PredictData data, int row, int col)
    {
        return data.segMask[row * data.maskWidth + col];
    }

    public float ComputeSteering()
    {
        if (lastPredictData == null) return 0;

        float sumX = 0;
        int count = 0;
        int startRow = lastPredictData.maskHeight / 4;

        for (int row = startRow; row < lastPredictData.maskHeight/4 + 16; row++)
        {
            for (int col = 0; col < lastPredictData.maskWidth; col++)
            {
                int v = GetMaskValue(lastPredictData, row, col);

                if (v == 0) // road
                {
                    sumX += col;
                    count++;
                }
            }
        }

        float centerX = (count > 0) ? sumX / count : lastPredictData.maskWidth / 2f;

        float screenCenter = lastPredictData.maskWidth / 2f;
        float error = centerX - screenCenter;

        return Mathf.Clamp(error / screenCenter, -1f, 1f);
    }

    public void ApplyControls(float steerValue)
    { 
        float targetSpeed = car.maxSpeed * (1 - steerValue);
        car.input.steerValue = steerValue;

        //float speedFactor = 1f - Mathf.Abs(steerValue);

        //if (speedFactor > 0.9f)
        //    car.input.accelerationValue = 0.3f;
        //else if (speedFactor > 0.6f)
        //    car.input.accelerationValue = 0.1f;
        //else
        //    car.input.accelerationValue = -0.2f;
        if (car.currentSpeed > targetSpeed) car.input.accelerationValue = -0.3f;
        else if (car.currentSpeed < targetSpeed) car.input.accelerationValue = 0.3f;
        else car.input.accelerationValue = 0f;
    }

    void OnGUI()
    {
        if (tex == null) return;

        int x = 10;
        int y = 10;
        int width = 320;
        int height = 240;

        // draw camera image
        GUI.DrawTexture(new Rect(x, y, width, height), tex);
        if (lastPredictData == null) return;

        float cellWidth = (float)width / lastPredictData.maskWidth;
        float cellHeight = (float)height / lastPredictData.maskHeight;

        for (int row = 0; row < lastPredictData.maskHeight; row++)
        {
            for (int col = 0; col < lastPredictData.maskWidth; col++)
            {
                int value = lastPredictData.segMask[row * lastPredictData.maskWidth + col];

                switch (value)
                {
                    case 0: GUI.color = new Color(0, 1, 0, 0.3f); break; // road
                    case 1: GUI.color = new Color(1, 0, 0, 0.3f); break; // sidewalk
                    case 2: GUI.color = new Color(1, 1, 1, 0.3f); break; //building;
                    case 13: GUI.color = Color.yellow; break; // car
                    default: GUI.color = new Color(0, 0, 1, 0.2f); break;
                }

                GUI.DrawTexture(
                    new Rect(
                        x + col * cellWidth,
                        y + row * cellHeight,
                        cellWidth,
                        cellHeight
                    ),
                    Texture2D.whiteTexture
                );
            }
        }
        GUI.color = Color.white;
    }
}