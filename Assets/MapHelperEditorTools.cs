using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class MapHelperEditorTools : EditorWindow
{
    [MenuItem("Window/小地图生成")]
    static void ShowWindow()
    {
        var tWin = CreateInstance<MapHelperEditorTools>();
        tWin.Show();
    }
    void OnDestroy()
    {
        Clean();
    }

    private void Clean()
    {
        if (mCam != null)
        {
            GameObject.DestroyImmediate(mCam.gameObject);
        }
    }

    private int mMapId = 0;
    private Vector2 mScreenSize = new Vector2(1280, 720);

    private Camera mCam;
    private int mCamXOffset = 0;
    private int mCamZOffset = 0;
    private int mCamSize = 18;
    private int mMapWidth = 128;
    private int mMapHeight = 0;

    private Vector3 mModelPos;
    private Vector3 mNGUIPos;
    private Vector3 mUIPos;

    private GameObject mShowUIGO;
    private UITexture mShowMapTexture;

    private GameObject mWorldGO;

    private string mResult;


    void OnGUI()
    {
        EditorGUILayout.BeginVertical("Box");
        mMapId = EditorGUILayout.IntField("场景ID：", mMapId);
        mScreenSize = EditorGUILayout.Vector3Field("屏幕分辨率:", mScreenSize);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("Box");
        mCamXOffset = EditorGUILayout.IntField("水平移动X：", mCamXOffset);
        mCamZOffset = EditorGUILayout.IntField("垂直移动Z：", mCamZOffset);
        mCamSize = EditorGUILayout.IntField("Size：", mCamSize);
        mCam = EditorGUILayout.ObjectField("俯视模型摄像机：", mCam, typeof(Camera), true) as Camera;
        mCam = UpdateCamera();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("Box");
        mMapWidth = EditorGUILayout.IntField("地图宽：", mMapWidth);
        mMapHeight = (int)(mMapWidth / (mScreenSize.x / mScreenSize.y));
        EditorGUILayout.LabelField("地图高：", mMapHeight.ToString());
        mWorldGO = EditorGUILayout.ObjectField("模型Go：", mWorldGO, typeof(GameObject), true) as GameObject;
        mShowUIGO = EditorGUILayout.ObjectField("模型Go->地图Icon：", mShowUIGO, typeof(GameObject), true) as GameObject;
        mShowMapTexture = EditorGUILayout.ObjectField("UI地图Texture：", mShowMapTexture, typeof(UITexture), true) as UITexture;
        if (GUILayout.Button("抓取", GUILayout.Width(60)))
        {

            if (mCam != null)
            {
                CamToPng(mCam, mMapWidth, mMapHeight);
                AssetDatabase.Refresh();
            }
        }
        EditorGUILayout.EndVertical();

        if (mWorldGO != null)
        {
            mModelPos = EditorGUILayout.Vector3Field("Unity Transform pos:", mWorldGO.transform.position);
            //mNGUIPos = EditorGUILayout.Vector3Field("NGUI Transform pos:", mNGUIPos);
            mUIPos = EditorGUILayout.Vector3Field("Map Transform pos:", mUIPos);

            UpdatUIInfo();
        }



        EditorGUILayout.SelectableLabel(mResult);


    }
    private void UpdatUIInfo()
    {

        mNGUIPos = UnityWorldToNGUIWorld(mModelPos);
        mUIPos = WorldPosToMiniMapPos(mModelPos);
        if (mShowUIGO != null)
            mShowUIGO.transform.position = mUIPos;

        mScale = mScreenSize.x / mMapWidth;
        mUnitOffset = CalOffset();

        mResult = string.Format(" 屏幕/地图比：{0},偏移X：{1},偏移Y：{2}\n图片保存路径：{3}", mScale.ToString(), mUnitOffset.x.ToString("f2"), mUnitOffset.y.ToString("f2"), savePath);

    }
    Camera mUICamera;
    private void OnEnable()
    {
        var cam = GameObject.FindObjectOfType<UICamera>();
        if (cam != null) mUICamera = cam.cachedCamera;
    }

    #region 计算
    private float mScale;//缩放
    private Vector2 mUnitOffset;//偏移

    private Vector3 CalOffset()
    {
        return UnityWorldToNGUIWorld(new Vector3(1, 0, 1));
    }
    private Vector3 UnityWorldToNGUIWorld(Vector3 worldPos)
    {
        //unity world 2 Screen
        Vector3 sPos = Camera.main.WorldToScreenPoint(worldPos);
        //unity Screen 2 NGUI world
        if (mUICamera == null) return Vector3.zero;
        return mUICamera.ScreenToWorldPoint(sPos);
    }

    private Vector3 WorldPosToMiniMapPos(Vector3 worldPos)
    {
        Vector3 screenPos = UnityWorldToNGUIWorld(worldPos);
        return new Vector3(screenPos.x / mScale, screenPos.y / mScale, 0);
    }

    #endregion

    #region  获取
    private string savePath;
    private void CamToPng(Camera mCam, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 2);
        mCam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.ARGB32, false);
        mCam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        mCam.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(rt);
        SaveTextureToPng(screenShot, Application.dataPath + "/temp", GetMapName());
    }

    public void SaveTextureToPng(Texture2D png, string path, string pngName)
    {
        byte[] bytes = png.EncodeToPNG();
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        savePath = path + "/" + pngName + ".png";
        FileStream file = File.Open(path + "/" + pngName + ".png", FileMode.Create);
        BinaryWriter writer = new BinaryWriter(file);
        writer.Write(bytes);
        file.Close();
        Texture2D.DestroyImmediate(png);
    }

    public string GetMapName()
    {
        return mMapId + "_" + mMapWidth + "x" + mMapHeight;
    }

    public Camera UpdateCamera()
    {
        if (mCam == null)
        {
            var camGo = new GameObject("MapCamera");
            mCam = camGo.AddComponent<Camera>();
            mCam.transform.localPosition = new Vector3(0, 50, 0);
            mCam.transform.eulerAngles = new Vector3(90f, 0f, 0f);
            mCam.transform.localScale = Vector3.one;
            mCam.orthographic = true;
            mCam.orthographicSize = mCamSize;

        }
        else
        {
            mCam.transform.localPosition = new Vector3(mCamXOffset, 50, mCamZOffset);

            mCam.orthographicSize = mCamSize;
        }
        return mCam;
    }

    #endregion

    #region 格式

    static public void DrawVertical(Action pChild, params GUILayoutOption[] pOptions)
    {
        if (pChild != null)
        {
            EditorGUILayout.BeginVertical(pOptions);
            pChild();
            EditorGUILayout.EndVertical();
        }
    }
    static public void DrawVertical(Action pChild, string pStyle, params GUILayoutOption[] pOptions)
    {
        if (pChild != null)
        {
            if (string.IsNullOrEmpty(pStyle)) EditorGUILayout.BeginHorizontal(pOptions);
            EditorGUILayout.BeginVertical(pStyle, pOptions);
            pChild();
            EditorGUILayout.EndVertical();
        }
    }
    static public void DrawHorizontal(Action pChild, params GUILayoutOption[] pOptions)
    {
        if (pChild != null)
        {
            EditorGUILayout.BeginHorizontal(pOptions);
            pChild();
            EditorGUILayout.EndHorizontal();
        }
    }
    static public void DrawHorizontal(Action pChild, string pStyle, params GUILayoutOption[] pOptions)
    {
        if (pChild != null)
        {
            if (string.IsNullOrEmpty(pStyle)) EditorGUILayout.BeginHorizontal(pOptions);
            EditorGUILayout.BeginHorizontal(pStyle, pOptions);
            pChild();
            EditorGUILayout.EndHorizontal();
        }
    }
    static public void DrawButton(string btnName, int btnWidth, Action action)
    {
        if (GUILayout.Button(btnName, GUILayout.Width(btnWidth)))
        {
            if (action != null) action();
        }
    }
    #endregion
}

public class MapTextureHelperTools : AssetPostprocessor
{
    public void OnPreprocessTexture()
    {
        TextureImporter textureImporter = (TextureImporter)assetImporter;
        textureImporter.npotScale = TextureImporterNPOTScale.None;
        Debug.Log("地图格式设置成功");
    }
}