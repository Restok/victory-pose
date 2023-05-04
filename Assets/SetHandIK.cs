using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetHandIK : MonoBehaviour
{
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    private Animator thisAnimator;
    private void Start(){
        thisAnimator = GetComponent<Animator>();
    }
    private void OnAnimatorIK(int layerIndex) {
        thisAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
        thisAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
        thisAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftHand.position);
        thisAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightHand.position);
    }
}
