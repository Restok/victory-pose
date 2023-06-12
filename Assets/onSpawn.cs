using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class onSpawn : NetworkBehaviour
{
    override public void OnNetworkSpawn(){
        this.transform.position = new Vector3(0,3,0);
    }
}
