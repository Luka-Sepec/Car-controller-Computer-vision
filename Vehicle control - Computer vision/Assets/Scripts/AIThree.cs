using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
public class AIThree : MonoBehaviour //DepthAreaV2
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
        public List<int> depthMap;
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

        //if (lastPredictData != null) DriveCar();
        
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
            lastPredictData = data;
            //Debug.Log("Lanes received:");
            //foreach (var point in data.lanes)
            //{
            //    Debug.Log($"Lane {point.laneIndex}: x={point.x}, y={point.y}");
            //}
            //Debug.Log(data.depthMap);

        }
        canSendFrame = true;
    }
    int GetDepth(int row, int col)
    {
        return lastPredictData.depthMap[row * lastPredictData.maskWidth + col];
    }

    void DriveCar()
    {
        if (lastPredictData.maskWidth == 0 || lastPredictData.maskHeight == 0) return;
        int height = lastPredictData.maskHeight;
        int width = lastPredictData.maskWidth;

        int startRow = height / 2;

        float bestScore = float.MaxValue;
        int bestCol = width / 2;

        for (int col = 0; col < width; col++)
        {
            float totalDepth = 0;
            int count = 0;

            for (int row = startRow; row < height; row++)
            {
                int depth = GetDepth(row, col);

                totalDepth += depth;
                count++;
            }
            if (count == 0) continue;

            float avgDepth = totalDepth / count;

            if (avgDepth < bestScore)
            {
                bestScore = avgDepth;
                bestCol = col;
            }
        }

        float screenCenter = width / 2f;
        float error = bestCol - screenCenter;

        float steer = Mathf.Clamp(error * 2f/ screenCenter, -1f, 1f);

        car.input.steerValue = steer;

        if (bestScore > 15)
            car.input.accelerationValue = 0.3f;
        else
            car.input.accelerationValue = -0.3f;
    }

    void OnGUI()
    {
        if (tex == null) return;

        int x = 10;
        int y = 10;
        int width = 320;
        int height = 240;

        GUI.DrawTexture(new Rect(x, y, width, height), tex);

        if (lastPredictData == null) return;

        float cellWidth = (float)width / lastPredictData.maskWidth;
        float cellHeight = (float)height / lastPredictData.maskHeight;

        for (int row = 0; row < lastPredictData.maskHeight; row++)
        {
            for (int col = 0; col < lastPredictData.maskWidth; col++)
            {
                int depth = GetDepth(row, col);

                float normalized = depth / 25f;

                GUI.color = new Color(
                    normalized,
                    1f - normalized,
                    0,
                    0.4f
                );

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