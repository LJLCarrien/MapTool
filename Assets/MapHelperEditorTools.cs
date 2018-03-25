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
    private int mMapId = 0;
    private Vector2 mScreenSize = new Vector2(1280, 720);

    private Camera mCam;
    private int mCamHeight = 128;
    private int mCamXOffset = 0;
    private int mCamZOffset = 0;
    private int mCamSize = 18;

    private RenderTexture mRenderTexture;
    private Texture mCamTakeTexture;

    private Texture mMapTexture;
    private int mMapWidth = 50;
    private int mMapHeight = 0;

    private Vector3 mModelPos;
    private Vector3 mNGUIPos;
    private Vector3 mUIPos;

    private GameObject mShowUIGo;


    private string mResult;


    void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        mMapId = EditorGUILayout.IntField("场景ID：", mMapId);
        mScreenSize = EditorGUILayout.Vector3Field("屏幕分辨率:", mScreenSize);

        mCamHeight = EditorGUILayout.IntField("相机高度H：", mCamHeight);
        mCamXOffset = EditorGUILayout.IntField("水平移动X：", mCamXOffset);
        mCamZOffset = EditorGUILayout.IntField("垂直移动Z：", mCamZOffset);
        mCamSize = EditorGUILayout.IntField("Size：", mCamSize);

        mCamTakeTexture = EditorGUILayout.ObjectField("摄像机俯拍：", mCamTakeTexture, typeof(Texture), true) as Texture;


        mMapWidth = EditorGUILayout.IntField("地图宽：", mMapWidth);
        mMapHeight = (int)(mMapWidth / (mScreenSize.x / mScreenSize.y));

        EditorGUILayout.LabelField("地图高：", mMapHeight.ToString());

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("地图预览：");
        mMapTexture = EditorGUILayout.ObjectField(mMapTexture, typeof(Texture), true, GUILayout.Width(mMapWidth), GUILayout.Height(mMapHeight)) as Texture;
        EditorGUILayout.EndHorizontal();


        mModelPos = EditorGUILayout.Vector3Field("Unnity position:", mModelPos);
        mNGUIPos = EditorGUILayout.Vector3Field("NGUI Transform position:", mNGUIPos);
        mShowUIGo = EditorGUILayout.ObjectField("地图UI直观显示：", mShowUIGo, typeof(GameObject), true) as GameObject;

        if (GUILayout.Button("抓取", GUILayout.Width(60)))
        {
            if (mCam == null)
                mCam = GetCamera();
            if (mCam != null)
            {
                MakeCameraImg(mCam, mMapWidth, mMapHeight);
                GameObject.DestroyImmediate(mCam.gameObject);
                AssetDatabase.Refresh();
            }
        }

        EditorGUILayout.EndVertical();

        k.v = mModelPos;

        UnityWorldToNGUIWorld();
        if (mShowUIGo != null)
            mShowUIGo.transform.position = new Vector3(mNGUIPos.x, mNGUIPos.y, 0);

        Vector2 unitOffset = CalOffset();
        mResult = string.Format(" 缩放{0},偏移X:{1},偏移Y:{2}", mScreenSize.x / mMapWidth, unitOffset.x.ToString("f2"), unitOffset.y.ToString("f2"));
        EditorGUILayout.SelectableLabel(mResult);
    }

    #region 计算

    private void UnityWorldToNGUIWorld()
    {
        //unity world 2 Screen
        Vector3 sPos = Camera.main.WorldToScreenPoint(mModelPos);
        //unity Screen 2 NGUI world
        if (mUICamera == null) return;
        mNGUIPos = mUICamera.ScreenToWorldPoint(sPos);
    }

    Camera mUICamera;
    private void OnEnable()
    {
        var cam = GameObject.FindObjectOfType<UICamera>();
        if (cam != null) mUICamera = cam.cachedCamera;
    }

    private Vector2 CalOffset()
    {
        //unity world 2 Screen
        Vector3 sPos = Camera.main.WorldToScreenPoint(new Vector3(1, 0, 1));
        //unity Screen 2 NGUI world
        if (mUICamera == null) return Vector2.zero;
        return mUICamera.ScreenToWorldPoint(sPos);
    }
    private float _scale = 72 / 10;
    private float mapOffsetX = 639f;
    private float mapOffsetY = 358f;
    private Vector3 ConverWorldPosToMinMapPos(float worldX, float worldZ)
    {
        return new Vector3(worldX * _scale + mapOffsetX, worldZ * _scale - mapOffsetY, 0);
    }
    private Transform m_Transform;
    private float mRadius = 1;
    public Color mColor = Color.green;
    public float m_Theta = 0.1f;

    private void DrawPos()
    {

    }
    #endregion

    #region  获取
    private void MakeCameraImg(Camera mCam, int width, int height)
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
        FileStream file = File.Open(path + "/" + pngName + ".png", FileMode.Create);
        BinaryWriter writer = new BinaryWriter(file);
        writer.Write(bytes);
        file.Close();
        Texture2D.DestroyImmediate(png);
    }

    public Texture2D GetTexture2D(RenderTexture renderT)
    {
        if (renderT == null)
            return null;

        int width = renderT.width;
        int height = renderT.height;
        Texture2D tex2D = new Texture2D(width, height, TextureFormat.ARGB32, false);
        RenderTexture.active = renderT;
        tex2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex2D.Apply();
        return tex2D;
    }

    //public Material GetMaterial()
    //{
    //    if (mMaterial == null)
    //    {
    //        //mMaterial = new Material("Shader \"MapHelperEditorTools / ForMapSharder\"" +
    //        //                         " {Properties{_MainTex (\"Texture\", 2D) = \"white\" {}}" +
    //        //    " SubShader{Tags { \"RenderType\" = \"Opaque\" }LOD 100" +
    //        //    "Pass{CGPROGRAM #pragma vertex vert #pragma fragment frag # include \"UnityCG.cginc\"" +
    //        //    "  struct appdata{float4 vertex : POSITION;float2 uv : TEXCOORD0;};" +
    //        //    "struct v2f{float2 uv : TEXCOORD0;float4 vertex : SV_POSITION;};" +
    //        //    " sampler2D _MainTex;float4 _MainTex_ST;" +
    //        //    " v2f vert(appdata v){v2f o;o.vertex = UnityObjectToClipPos(v.vertex);o.uv = TRANSFORM_TEX(v.uv, _MainTex);return o;}" +
    //        //    "fixed4 frag(v2f i) : SV_Target{fixed4 col = tex2D(_MainTex, i.uv);return col;}ENDCG}}}");

    //        mMaterial = new Material(Shader.Find("MapHelperEditorTools/ForMapSharder"));
    //    }
    //    return mMaterial;
    //}

    public string GetMapName()
    {
        return mMapId + "_" + mMapWidth + "x" + mMapHeight;
    }

    public Camera GetCamera()
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