using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPhysicalBody : MonoBehaviour
{
    public Transform body;
    [SerializeField] LayerMask terrainLayer = default;
    // Update is called once per frame
    void FixedUpdate()
    {
        Ray ray = new Ray(body.position, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit info, 10, terrainLayer.value)){
            this.transform.position = info.point;
        }
    }
}
