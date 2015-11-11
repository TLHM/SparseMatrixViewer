using UnityEngine;
using System.Collections;

/** Responsible for rotating on mouse drag */
public class RotateOnMouseDrag : MonoBehaviour {
	public Transform t;			/**< Transform to rotate */
	public float pixPerDeg;		/**< Conversion of movement in pixels to movement in degrees */
	public float rotScale;		/**< Speed of rotation when using arrow keys */
	Vector2 mouseClickPos;		/**< Where the mouse was initially clicked down */
	bool mouseDown;				/**< Is the mouse dragging right now? */
	Quaternion rot;				/**< Saved rotation of the transform when the mouse was clicked */

	/**
		Saves Rotation on mouse down, rotates depending on deltaMousePosition
		Can rotate with arrow keys, in case mouse is being odd
	*/
	void Update () {
		if(!Controller.loaded) return;

		//Save info on mouse down
		if(Input.GetMouseButtonDown(0)){
			rot = t.rotation;

			mouseClickPos = Input.mousePosition;
			mouseDown=true;
		}

		//Rotate if mouse is dragging
		if(mouseDown){
			Vector2 dif = (Vector2)Input.mousePosition-mouseClickPos;

			//Reset rotation to rot, then rotate depending on mouse movement
			t.rotation = rot;
			t.Rotate(new Vector3(dif.y/pixPerDeg,-dif.x/pixPerDeg,0),Space.World);
		}

		//Mouse isn't dragging anymore
		if(Input.GetMouseButtonUp(0)){
			mouseDown = false;
		}

		//Arrow key rotation
		if(Input.GetKey(KeyCode.RightArrow)){
			t.Rotate(Vector3.up*rotScale, Space.World);
		}
		if(Input.GetKey(KeyCode.LeftArrow)){
			t.Rotate(-Vector3.up*rotScale, Space.World);
		}
		if(Input.GetKey(KeyCode.UpArrow)){
			t.Rotate(-Vector3.right*rotScale, Space.World);
		}
		if(Input.GetKey(KeyCode.DownArrow)){
			t.Rotate(Vector3.right*rotScale, Space.World);
		}
	}
}
