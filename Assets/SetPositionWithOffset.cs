using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetPositionWithOffset : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;
    void Update()
    {
        this.transform.position = target.position+target.transform.forward+offset;
    }
}
