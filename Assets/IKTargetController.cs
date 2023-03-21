using System;
using System.Collections;
using System.Collections.Generic;
using Mediapipe;
using UnityEngine;

public class IKTargetController : MonoBehaviour
{
    [SerializeField] private float maxVelocity = 1;
    private Vector2 nosePos;

    private Rigidbody rigidbody;
    private Vector3 movementVelocity = new Vector3();
    NormalizedLandmarkList poseLandmarks;

    private void Start(){
       rigidbody = GetComponent<Rigidbody>();
    }
    internal void setPoseLandmark(NormalizedLandmarkList poseLandmarks)
    {
        this.poseLandmarks = poseLandmarks;
        if(poseLandmarks!= null) {
            NormalizedLandmark nose = poseLandmarks.Landmark[0];
            setLocomotionState(nose);
        }
    }
    private void Update() {
        print(movementVelocity);
        rigidbody.velocity = movementVelocity;
    }
    private void setLocomotionState(NormalizedLandmark headPos) {
        int movingForward = 0;
        int movingRight = 0;
        if(headPos.Y > .7f) {
            movingForward = -1;
            print("movingBack");
        }
        else if (headPos.Y < 0.3f) {
            movingForward = 1;
        }
        if(headPos.X > 0.7f) {
            movingRight = -1;
        }
        else if(headPos.X < 0.3f){
            movingRight = 1;
        }
        
        
        float forwardVelocity = movingForward * maxVelocity * Math.Abs(headPos.Y - 0.5f)*2;
        float rightVelocity = movingRight * maxVelocity * Math.Abs(headPos.X - 0.5f)*2;
        movementVelocity = Vector3.Normalize(forwardVelocity * Vector3.forward + Vector3.right*rightVelocity);
        print(movementVelocity);
        movementVelocity *= Math.Max(Math.Abs(forwardVelocity), Math.Abs(rightVelocity));        
    }
    
    
}
