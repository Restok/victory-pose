using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetPositionToTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    

    // Update is called once per frame
    void Update()
    {
        this.transform.position = target.position;    
    }
}
