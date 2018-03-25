using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class k : MonoBehaviour
{
    static public Vector3 v;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(v, 5);
        Debug.Log(v);

    }
}
