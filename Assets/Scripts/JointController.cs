using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class JointController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Rigidbody upComponent;
    [SerializeField] private Rigidbody[] downComponents;
    [SerializeField] private float upforceMultiplier=200f;
    [SerializeField] private float downForceMultiplier=100f;
    private ConfigurableJoint[] playerJoints;
    private Quaternion[] initialJointsRotation;

    [SerializeField] private Transform animatedRoot;
    private Transform[] animatedTransforms;
    private Collider[] allColliders;
    public PhysicMaterial zeroFrictionMat;
    void Start()
    {

        allColliders = GetComponentsInChildren<Collider>();
        
        playerJoints = GetComponentsInChildren<ConfigurableJoint>();
        initialJointsRotation = new Quaternion[playerJoints.Length];
        for (int i = 0; i < allColliders.Length; i++) {
            allColliders[i].material = zeroFrictionMat;
        }
        for (int i = 0; i < playerJoints.Length; i++) {
            initialJointsRotation[i] = playerJoints[i].transform.localRotation;
            if(playerJoints[i].tag == "Hip") continue;
            ConfigurableJointExtensions.SetupAsCharacterJoint(playerJoints[i]);
        }
        animatedTransforms = new Transform[playerJoints.Length];   
        for(int i = 0; i<playerJoints.Length; i++){
            animatedTransforms[i] = RecursiveFindChild(animatedRoot, playerJoints[i].name);
        }

    }
    private void UpdateJointAngles(){
        if(playerJoints!=null){
            for (int i = 0; i < playerJoints.Length; i++) {
                ConfigurableJointExtensions.SetTargetRotationLocal(playerJoints[i], animatedTransforms[i].localRotation, initialJointsRotation[i]); 
            }    
        }
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        upComponent.AddForce(Vector3.up*upforceMultiplier, ForceMode.Force);
        foreach(Rigidbody r in downComponents){
            r.AddForce(Vector3.down*downForceMultiplier, ForceMode.Force);
        }
        UpdateJointAngles();
    }

    Transform RecursiveFindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if(child.name == childName)
            {
                return child;
            }
            else
            {
                Transform found = RecursiveFindChild(child, childName);
                if (found != null)
                {
                        return found;
                }
            }
        }
        return null;
    }
}
