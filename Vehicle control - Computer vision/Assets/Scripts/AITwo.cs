using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
public class AITwo : MonoBehaviour //DrivableAreaSegmentation
{
    public Camera CVCam;
    public Car car;
    public RenderTexture rt;
    public Texture2D tex;
    public bool canSendFrame = true;
    private PredictData lastPredictData = null; 

    [System.Serializable]
    public class PredictData
    {
        public List<int> drivableMask;
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
        UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:5000/predict", "POST");
        www.uploadHandler = new UploadHandlerRaw(bytes);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/octet-stream");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            var data = JsonUtility.FromJson<PredictData>(www.downloadHandler.text);
            lastPredictData = data;
            DriveCar(data);

        }
        canSendFrame = true;
    }

    public int GetMaskValue(PredictData data, int row, int col)
    {
        return data.drivableMask[row * data.maskWidth + col];
    }

    public void DriveCar(PredictData data)
    {
        int startRow = data.maskHeight - 6;
        float totalX = 0f;
        int count = 0;

        for (int row = startRow; row < data.maskHeight; row++)
        {
            for (int col = 0; col < data.maskWidth; col++)
            {
                int value = GetMaskValue(data, row, col);
                if (value == 1)
                {
                    totalX += col;
                    count++;
                }
            }
        }

        float targetX;

        if (count > 0) targetX = totalX / count;
        else targetX = data.maskWidth / 2f;

        float centerX = data.maskWidth / 2f;
        float error = targetX - centerX;
        float steerValue = Mathf.Clamp(error * 2f / centerX, -1f, 1f);
        //car.input.steerValue = steerValue;

        float speedFactor = 1f - Mathf.Abs(steerValue);

        //if (speedFactor > 0.7f)
            //car.input.accelerationValue = 0.3f;
        //else if (speedFactor > 0.4f)
            //car.input.accelerationValue = 0.1f;
        //else
            //car.input.accelerationValue = -0.2f;
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
                int value = lastPredictData.drivableMask[row * lastPredictData.maskWidth + col];

                if (value == 1)
                    GUI.color = new Color(0, 1, 0, 0.3f);
                else
                    GUI.color = new Color(1, 0, 0, 0.3f);

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

