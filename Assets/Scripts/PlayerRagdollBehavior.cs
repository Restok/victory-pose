using UnityEngine;
using System.Collections;

namespace RootMotion.Dynamics {

	/// <summary>
	/// This is just a commented template for creating new Puppet Behaviours.
	/// </summary>
	public class PlayerRagdollBehavior : BehaviourBase {

        // Used for switching behaviours with PuppetEvents
        protected override string GetTypeSpring()
        {
            return typeSpring;
        }
		private MuscleCollisionBroadcaster broadcaster;

        // Change this to the name of your behaviour class.
        private const string typeSpring = "PlayerRagdollBehavior";

        // Just for demonstrating the intended use of sub-behaviours. 
        // Sub-behaviours take care of behaviour code reusability.
        // While there can be only one active Puppet Behaviour at a time, that active behaviour can use multiple independent and reusable sub-behaviours simultaneously.
        // For example SubBehaviourCOM is responsible for calculating everything about the center of mass and can be used by any behaviour or even other sub-behaviours that need CoM information.
        public float collisionThreshold = 0;
        private int collisions;
        public SubBehaviourCOM centerOfMass;

		// Used by SubBehaviourCOM
		public LayerMask groundLayers;

		// Just for demonstrating the intended use of PuppetEvents
		public PuppetEvent onLoseBalance;

		// A PuppetEvent will be called when the balance angle exceeds this point.
		public float loseBalanceAngle = 60f;

		protected override void OnInitiate() {
			// Initiate something. This is called only once by the PuppetMaster in Start().

			// Initiating sub-behaviours. SubBehaviourCOM will update automatically once it has been initiated
			centerOfMass.Initiate(this as BehaviourBase, groundLayers);
		}
		
		protected override void OnActivate() {
			// When this becomes the active behaviour. There can only be one active behaviour. 
			// Switching behaviours is done by the behaviours themselves, using PuppetEvents.
			// Each behaviour should know when it is no longer required and which behaviours to switch to in each case.
		}

		public override void OnReactivate() {
			// Called when the PuppetMaster has been deactivated (by parenting it to an inactive hierarchy or calling SetActive(false)).
		}

		protected override void OnDeactivate() {
			// Called when this behaviour is exited. OnActivate is the place for resetting variables to defaults though.
		}

		protected override void OnFixedUpdate(float deltaTime) {
			// Everything happening in the fixed time step.

		}

		protected override void OnLateUpdate(float deltaTime) {
			// Everything happening in LateUpdate().
		}

// When a muscle is hit (through MuscleCollisionBroadcaster.Hit(...))
		protected override void OnMuscleHitBehaviour(MuscleHit hit) {
            Debug.Log("Onmusclehit");
			puppetMaster.muscles[hit.muscleIndex].rigidbody.AddForceAtPosition(hit.force, hit.position);
            
		}

        // When a muscle collides with something (called by the MuscleCollisionBroadcaster component on the muscle).
        protected override void OnMuscleCollisionBehaviour(MuscleCollision m) {   
                Debug.Log("OnmuscleCollision");
                float cT = collisionThreshold;

                float impulse = GetImpulse(m, ref cT);

                float minImpulseMlp = PuppetMasterSettings.instance != null ? (1f + PuppetMasterSettings.instance.currentlyActivePuppets * PuppetMasterSettings.instance.activePuppetCollisionThresholdMlp) : 1f;
                float minImpulse = cT * minImpulseMlp;

                if (impulse <= minImpulse) return;
                collisions++;

                // Try to find out if it collided with another puppet's muscle
                if (m.collision.collider.attachedRigidbody != null)
                {
                    Debug.Log("collided by someone");
                    broadcaster = m.collision.collider.attachedRigidbody.GetComponent<MuscleCollisionBroadcaster>();
                    if (broadcaster != null)
                    {
                        if (broadcaster.muscleIndex < broadcaster.puppetMaster.muscles.Length)
                        {
                            // Multiply impulse (if the other muscle has been boosted)
                            impulse *= broadcaster.puppetMaster.muscles[broadcaster.muscleIndex].state.impulseMlp;

                            //float stayF = m.isStay? 0.05f: 0.1f;
                            //broadcaster.puppetMaster.muscles[broadcaster.muscleIndex].offset -= m.collision.impulse * Time.deltaTime * stayF;
                        }
                    }
                }

		}
		private float GetImpulse(MuscleCollision m, ref float layerThreshold) {
			float i = m.collision.impulse.sqrMagnitude;

			i /= puppetMaster.muscles [m.muscleIndex].rigidbody.mass;
			i *= 0.3f; // Coeficient for evening out for pre-0.3 versions
			return i;
		}
        protected void MoveTarget(Vector3 position) {
			puppetMaster.targetRoot.position = position;
		}
	}
}
