using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ttt : MonoBehaviour
{

    public Transform t;
    public Vector3 worldPos;
    public Transform tui;

    [ContextMenu("tt")]
    void test()
    {
        ////unity world 2 Screen
        //Vector3 sPos = Camera.main.WorldToScreenPoint(t.position);
        ////unity Screen 2 NGUI world
        //Vector3 v = UICamera.currentCamera.ScreenToWorldPoint(sPos);
        //tui.position = new Vector3(v.x,v.y,0); 
        //Debug.Log(v);

        Debug.Log(Screen.height);
        Debug.Log(Screen.width);

    }

    private void OnGUI()
    {
        GUILayout.Label("pos:" + Input.mousePosition);
    }

}
