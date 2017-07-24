using UnityEngine;
using System.IO;

public class ShowImageOnPanel : MonoBehaviour {

    public GameObject ImageFrameObject; // The object to place the image on

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	} 

    public void DisplayImage()
    {
        Texture2D imageTxtr = new Texture2D(2, 2);
        string fileName = gameObject.GetComponent<ImageToVisionAPI>().fileName;
        byte[] fileData = File.ReadAllBytes(fileName);
        imageTxtr.LoadImage(fileData);
        ImageFrameObject.GetComponent<Renderer>().material.mainTexture = imageTxtr;
    }
}
