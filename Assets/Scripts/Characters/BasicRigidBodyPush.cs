using UnityEngine;

namespace Wingmann.Project.Characters
{
	public class BasicRigidBodyPush : MonoBehaviour
	{
		public LayerMask PushLayers;
		public bool CanPush;

		[Range(0.5f, 5.0f)]
		public float Strength = 1.1f;

		private void OnControllerColliderHit(ControllerColliderHit hit)
		{
			if (CanPush)
			{
				PushRigidBodies(hit);
			}
		}

		private void PushRigidBodies(ControllerColliderHit hit)
		{
			// https://docs.unity3d.com/ScriptReference/CharacterController.OnControllerColliderHit.html

			// Make sure we hit a non kinematic rigidbody.
			var body = hit.collider.attachedRigidbody;

			if (body is null || body.isKinematic)
			{
				return;
			}

			// Make sure we only push desired layer(s).
			var bodyLayerMask = 1 << body.gameObject.layer;

			if ((bodyLayerMask & PushLayers.value) is 0)
			{
				return;
			}

			// We dont want to push objects below us
			if (hit.moveDirection.y < -0.3f)
			{
				return;
			}

			// Calculate push direction from move direction, horizontal motion only.
			var pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);

			// Apply the push and take strength into account.
			body.AddForce(pushDir * Strength, ForceMode.Impulse);
		}
	}
}
