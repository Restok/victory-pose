using System;
using Cinemachine;
using Mediapipe;
using Mediapipe.Unity.PoseTracking;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovementControllerRB : NetworkBehaviour
{
    [SerializeField] private Rigidbody playerHip;

    [SerializeField] private float maxVelocity = 2.5f;
    private InterpolatingVector3 interpolatingVelocity = new InterpolatingVector3(new Vector3(0,0,0),new Vector3(0,0,0), 0.2f);

    private Vector2 nosePos;
    private Animator anim;
    private Vector3 forward;
    private Vector3 right;
    private NormalizedLandmark headPos;
    private NormalizedLandmark leftHandPos;
    private NormalizedLandmark rightHandPos;
    private Vector2 middleShoulderPos;

    private float maxOffsetFromCenterX = 1.67f;
    private float maxOffsetFromCenterY = 1f;
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;
    [SerializeField] private Transform playerHead;
    private CinemachineVirtualCamera _cinemachineVirtualCamera;
    private GameObject _mainCamera;
    private PoseTrackingSolution _poseTrackingSolution;
    private class InterpolatingVector3 {
        public Vector3 currentValue {get;set;}
        public Vector3 targetValue {get;set;}
        public float rate {get;set;}

        public InterpolatingVector3(Vector3 currentValue, Vector3 targetValue, float rate=0.05f)
        {
            this.currentValue = currentValue;
            this.targetValue = targetValue;
            this.rate = rate;
        }
        public void UpdateVariable(){
            this.currentValue = Vector3.Lerp(currentValue, targetValue, rate);
        }
    }
    // private class InterpolatingFloat {
    //     public float currentValue {get;set;}
    //     public float targetValue {get;set;}
    //     public float rate {get;set;}

    //     public InterpolatingFloat(float currentValue, float targetValue, float rate=0.3f)
    //     {
    //         this.currentValue = currentValue;
    //         this.targetValue = targetValue;
    //         this.rate = rate;
    //     }
    //     public void UpdateVariable(){
    //         this.currentValue += (this.targetValue-this.currentValue)*rate;
    //     }
    // }
    private void Awake(){
        if (_mainCamera == null) {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
        if(_cinemachineVirtualCamera == null){
            _cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        }
        if(_poseTrackingSolution == null){
            _poseTrackingSolution = FindObjectOfType<PoseTrackingSolution>();
        }

    }
    private void Start(){
        anim = GetComponent<Animator>();
    }
    public override void OnNetworkSpawn(){
        base.OnNetworkSpawn();
        if (IsClient && IsOwner) {
            _cinemachineVirtualCamera.Follow = transform;
            _cinemachineVirtualCamera.LookAt = playerHead;
            _poseTrackingSolution.PlayerMovementController = GetComponent<PlayerMovementControllerRB>();
            _poseTrackingSolution.enabled = true;
        }
    }
    internal void setPoseLandmark(NormalizedLandmarkList poseLandmarks)
    {
        if(poseLandmarks!= null) {
            headPos = poseLandmarks.Landmark[0];
            leftHandPos = poseLandmarks.Landmark[15];
            rightHandPos = poseLandmarks.Landmark[16];
            //Average of left and right shoulder
            middleShoulderPos= new Vector2((poseLandmarks.Landmark[11].X + poseLandmarks.Landmark[12].X)/2, 
            (poseLandmarks.Landmark[11].Y + poseLandmarks.Landmark[12].Y)/2);
        }
    }

    private void FixedUpdate() {
        if(!IsOwner) {
            return;
        }
        if(headPos!= null){
            setLocomotionState();
        }
        if(leftHandPos!=null){
            setArmState();
        }
}
    private void setLocomotionState() {
        int movingForward = 0;
        int movingRight = 0;
        if(headPos.Y > .75f) {
            movingForward = -1;
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
        movePlayerServerRPC(movingForward, movingRight);

        
    }
    
    private void setArmState(){
        if(leftHandPos.X < rightHandPos.X) {
            (leftHandPos, rightHandPos) = (rightHandPos, leftHandPos);
        }
        Vector3 leftHandTargetPos = Vector3.Lerp(leftHandTarget.localPosition,getIKHandPosition(leftHandPos), 0.3f);
        Vector3 rightHandTargetPos = Vector3.Lerp(rightHandTarget.localPosition,getIKHandPosition(rightHandPos), 0.3f);
        movePlayerArmServerRPC(leftHandTargetPos, rightHandTargetPos);
        
    }
    private Vector3 getIKHandPosition(NormalizedLandmark handPos) {
        Vector2 handToMiddle = new Vector2((float) Math.Clamp(handPos.X - middleShoulderPos.x, -0.5, 0.5), (float) Math.Clamp(handPos.Y - middleShoulderPos.y, -0.5, 0.5));
        float handX = handToMiddle.x * 2.1f * -maxOffsetFromCenterX;
        float handY = handToMiddle.y * 2.1f * -maxOffsetFromCenterY;
        float handZ = (Math.Abs(Math.Abs(handToMiddle.x)-0.8f))*(Math.Abs(Math.Abs(handToMiddle.y)- 0.5f))*5;
        return new Vector3(handX, handY, handZ);    
    }
    [ServerRpc]
    private void movePlayerServerRPC(int movingForward, int movingRight){
        interpolatingVelocity.UpdateVariable();
        int numDirections = Math.Abs(movingForward) + Math.Abs(movingRight);
        float multiplier = maxVelocity;
        if(numDirections == 2) {
            multiplier = (float) Math.Sqrt(maxVelocity);
        }
        forward = transform.forward;
        right = transform.right;
        float forwardVelocity = movingForward * multiplier;
        float rightVelocity = movingRight * multiplier;
        Vector3 movementVelocity = forwardVelocity * forward + right*rightVelocity;
        float lrVelocity = Vector3.Dot(interpolatingVelocity.currentValue, right);
        float fbVelocity =  Vector3.Dot(interpolatingVelocity.currentValue, forward);

        interpolatingVelocity.targetValue = movementVelocity;
        Vector3 rbVelocity = playerHip.velocity;
        Vector3 velocityDiff = interpolatingVelocity.currentValue - rbVelocity;
        anim.SetFloat("LeftRightVelocity", lrVelocity);
        anim.SetFloat("ForwardBackwardVelocity", fbVelocity);
        playerHip.AddForce(velocityDiff, ForceMode.VelocityChange);
    }
    [ServerRpc]
    private void movePlayerArmServerRPC(Vector3 left, Vector3 right){
        leftHandTarget.localPosition = left;
        rightHandTarget.localPosition = right;
    }
}

