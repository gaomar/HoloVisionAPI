using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using HoloToolkit.Unity.InputModule;
using UnityEngine.UI;
using UnityEngine.VR.WSA.WebCam;
using System;

#if UNITY_UWP
using System.Linq;
#endif

public class ImageToVisionAPI : MonoBehaviour, IInputClickHandler
{

    public string url = "https://vision.googleapis.com/v1/images:annotate?key=";
    public string apiKey = "";

    public string fileName { get; private set; }
    string responseData;

    private ShowImageOnPanel panel;
    public Text text;              
    public FeatureType featureType = FeatureType.FACE_DETECTION;
    public int maxResults = 10;

    PhotoCapture photoCaptureObject = null;

    [System.Serializable]
    public class AnnotateImageRequests
    {
        public List<AnnotateImageRequest> requests;
    }

    [System.Serializable]
    public class AnnotateImageRequest
    {
        public Image image;
        public List<Feature> features;
    }

    [System.Serializable]
    public class Image
    {
        public string content;
    }

    [System.Serializable]
    public class Feature
    {
        public string type;
        public int maxResults;
    }

    [System.Serializable]
    public class ImageContext
    {
        public LatLongRect latLongRect;
        public List<string> languageHints;
    }

    [System.Serializable]
    public class LatLongRect
    {
        public LatLng minLatLng;
        public LatLng maxLatLng;
    }

    [System.Serializable]
    public class AnnotateImageResponses
    {
        public List<AnnotateImageResponse> responses;
    }

    [System.Serializable]
    public class AnnotateImageResponse
    {
        public List<FaceAnnotation> faceAnnotations;
        public List<EntityAnnotation> landmarkAnnotations;
        public List<EntityAnnotation> logoAnnotations;
        public List<EntityAnnotation> labelAnnotations;
        public List<EntityAnnotation> textAnnotations;
    }

    [System.Serializable]
    public class FaceAnnotation
    {
        public BoundingPoly boundingPoly;
        public BoundingPoly fdBoundingPoly;
        public List<Landmark> landmarks;
        public float rollAngle;
        public float panAngle;
        public float tiltAngle;
        public float detectionConfidence;
        public float landmarkingConfidence;
        public string joyLikelihood;
        public string sorrowLikelihood;
        public string angerLikelihood;
        public string surpriseLikelihood;
        public string underExposedLikelihood;
        public string blurredLikelihood;
        public string headwearLikelihood;
    }

    [System.Serializable]
    public class EntityAnnotation
    {
        public string mid;
        public string locale;
        public string description;
        public float score;
        public float confidence;
        public float topicality;
        public BoundingPoly boundingPoly;
        public List<LocationInfo> locations;
        public List<Property> properties;
    }

    [System.Serializable]
    public class BoundingPoly
    {
        public List<Vertex> vertices;
    }

    [System.Serializable]
    public class Landmark
    {
        public string type;
        public Position position;
    }

    [System.Serializable]
    public class Position
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class Vertex
    {
        public float x;
        public float y;
    }

    [System.Serializable]
    public class LocationInfo
    {
        LatLng latLng;
    }

    [System.Serializable]
    public class LatLng
    {
        float latitude;
        float longitude;
    }

    [System.Serializable]
    public class Property
    {
        string name;
        string value;
    }

    public enum FeatureType
    {
        TYPE_UNSPECIFIED,
        FACE_DETECTION,
        LANDMARK_DETECTION,
        LOGO_DETECTION,
        LABEL_DETECTION,
        TEXT_DETECTION,
        SAFE_SEARCH_DETECTION,
        IMAGE_PROPERTIES
    }

    public enum LandmarkType
    {
        UNKNOWN_LANDMARK,
        LEFT_EYE,
        RIGHT_EYE,
        LEFT_OF_LEFT_EYEBROW,
        RIGHT_OF_LEFT_EYEBROW,
        LEFT_OF_RIGHT_EYEBROW,
        RIGHT_OF_RIGHT_EYEBROW,
        MIDPOINT_BETWEEN_EYES,
        NOSE_TIP,
        UPPER_LIP,
        LOWER_LIP,
        MOUTH_LEFT,
        MOUTH_RIGHT,
        MOUTH_CENTER,
        NOSE_BOTTOM_RIGHT,
        NOSE_BOTTOM_LEFT,
        NOSE_BOTTOM_CENTER,
        LEFT_EYE_TOP_BOUNDARY,
        LEFT_EYE_RIGHT_CORNER,
        LEFT_EYE_BOTTOM_BOUNDARY,
        LEFT_EYE_LEFT_CORNER,
        RIGHT_EYE_TOP_BOUNDARY,
        RIGHT_EYE_RIGHT_CORNER,
        RIGHT_EYE_BOTTOM_BOUNDARY,
        RIGHT_EYE_LEFT_CORNER,
        LEFT_EYEBROW_UPPER_MIDPOINT,
        RIGHT_EYEBROW_UPPER_MIDPOINT,
        LEFT_EAR_TRAGION,
        RIGHT_EAR_TRAGION,
        LEFT_EYE_PUPIL,
        RIGHT_EYE_PUPIL,
        FOREHEAD_GLABELLA,
        CHIN_GNATHION,
        CHIN_LEFT_GONION,
        CHIN_RIGHT_GONION
    };

    public enum Likelihood
    {
        UNKNOWN,
        VERY_UNLIKELY,
        UNLIKELY,
        POSSIBLE,
        LIKELY,
        VERY_LIKELY
    }

    // Use this for initialization
    void Start()
    {
        InputManager.Instance.PushFallbackInputHandler(gameObject);
        panel = gameObject.GetComponent<ShowImageOnPanel>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    IEnumerator GetVisionFromImages()
    {
        // 解析開始
        Debug.Log("GetVisionFromImages　スタート!");
        text.text = "解析開始";

        if (apiKey == null || apiKey == "")
            Debug.LogError("No API key. Please set your API key into the \"Web Cam Texture To Cloud Vision(Script)\" component.");

        // 撮影したデータを取得しbase64に変換する
        byte[] bytes = File.ReadAllBytes(fileName);
        string base64 = Convert.ToBase64String(bytes);

        var headers = new Dictionary<string, string>() {
            { "Content-Type", "application/json;" }
        };

        AnnotateImageRequests requests = new AnnotateImageRequests();
        requests.requests = new List<AnnotateImageRequest>();

        AnnotateImageRequest request = new AnnotateImageRequest();
        request.image = new Image();
        request.image.content = base64;
        request.features = new List<Feature>();

        Feature feature = new Feature();
        feature.type = this.featureType.ToString();
        feature.maxResults = this.maxResults;

        request.features.Add(feature);

        requests.requests.Add(request);

        string jsonData = JsonUtility.ToJson(requests, false);
        if (jsonData != string.Empty)
        {
            // Google Vision APIに解析を依頼する
            string url = this.url + this.apiKey;
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);
            using (WWW www = new WWW(url, postData, headers))
            {
                yield return www;
                if (www.error == null)
                {
                    responseData = www.text;
                    Debug.Log("GetEmotionFromImages　終了!");
                    text.text = "";
                    Debug.Log(responseData.Replace("\n", "").Replace(" ", ""));
                    AnnotateImageResponses responses = JsonUtility.FromJson<AnnotateImageResponses>(responseData);
                    Sample_OnAnnotateImageResponses(responses);

                }
                else
                {
                    Debug.Log("Error: " + www.error);
                }
            }
        }

    }

    void Sample_OnAnnotateImageResponses(AnnotateImageResponses responses)
    {
        // Vision APIから返ってきた解析情報を展開
        if (responses.responses.Count > 0)
        {
            if (responses.responses[0].faceAnnotations != null && responses.responses[0].faceAnnotations.Count > 0)
            {
                text.text = "joyLikelihood: " + responses.responses[0].faceAnnotations[0].joyLikelihood;
            } else if (responses.responses[0].labelAnnotations != null && responses.responses[0].labelAnnotations.Count > 0)
            {
                // 解析して取得出来た文字列を列挙する
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                foreach (EntityAnnotation val in responses.responses[0].labelAnnotations) {
                    sb.AppendFormat("{0},", val.description);
                }
                text.text = "labelAnnotations: " + sb.ToString();

            } else if (responses.responses[0].textAnnotations != null && responses.responses[0].textAnnotations.Count > 0)
            {
                text.text = "TextAnnotations: " + responses.responses[0].textAnnotations[0].description;
            }
            else if (responses.responses[0].landmarkAnnotations != null && responses.responses[0].landmarkAnnotations.Count > 0)
            {
                text.text = "landmarkAnnotations: " + responses.responses[0].landmarkAnnotations[0].description;
            }
            else if (responses.responses[0].logoAnnotations != null && responses.responses[0].logoAnnotations.Count > 0)
            {
                text.text = "logoAnnotations: " + responses.responses[0].logoAnnotations[0].description;
            } else
            {
                text.text = "解析出来ませんでした...";
            }
        } else
        {
            text.text = "解析出来ませんでした...";
        }
    }

    // Air Tap処理
    public void OnInputClicked(InputClickedEventData eventData)
    {
        text.text = "撮影中...";

#if UNITY_UWP
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
#endif
    }

#if UNITY_UWP
    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        // 今映っているキャプチャを撮影

        photoCaptureObject = captureObject;

        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.BGRA32;

        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            // 撮影が終われば、キャッシュパスに画像を保存する
            fileName = string.Format(@"CapturedImage{0}_n.jpg", Time.time);
            fileName = Path.Combine(Application.temporaryCachePath, fileName);
            Debug.LogFormat("filePath={0}", fileName);
            photoCaptureObject.TakePhotoAsync(fileName, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
        }
    }

    void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            // 保存成功後、解析開始する
            Debug.Log("Saved Photo to disk!");
            panel.DisplayImage();
            StartCoroutine(GetVisionFromImages());
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }
        else
        {
            Debug.Log("Failed to save Photo to disk");
        }
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
#endif
}
