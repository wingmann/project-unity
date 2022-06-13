using UnityEngine;
using UnityEngine.InputSystem;

namespace Wingmann.Project.InputManagement
{
	public class Inputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 Move;
		public Vector2 Look;
		public bool Jump;
		public bool Sprint;

		[Header("Movement Settings")]
		public bool AnalogMovement;

		[Header("Mouse Cursor Settings")]
		public bool CursorLocked = true;
		public bool CursorInputForLook = true;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public void OnMove(InputValue value) => MoveInput(value.Get<Vector2>());

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public void OnLook(InputValue value)
		{
			if (CursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public void OnJump(InputValue value) => JumpInput(value.isPressed);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public void OnSprint(InputValue value) => SprintInput(value.isPressed);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="newMoveDirection"></param>
		public void MoveInput(Vector2 newMoveDirection) => Move = newMoveDirection;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="newLookDirection"></param>
		public void LookInput(Vector2 newLookDirection) => Look = newLookDirection;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="newJumpState"></param>
		public void JumpInput(bool newJumpState) => Jump = newJumpState;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="newSprintState"></param>
		public void SprintInput(bool newSprintState) => Sprint = newSprintState;

		//
		private void OnApplicationFocus(bool hasFocus) => SetCursorState(CursorLocked);

		// 
		private void SetCursorState(bool newState) =>
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
	}
}
