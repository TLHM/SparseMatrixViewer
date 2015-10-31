using UnityEngine;
using System.Collections;

public class RotateOnMouseDrag : MonoBehaviour {
	public Transform t;
	public float pixPerDeg;
	public float rotScale;
	Vector2 mouseClickPos;
	bool mouseDown;
	Quaternion rot;

	/** Saves Rotation on mouse down, rotates depending on deltaMousePosition
		Can rotate with arrow keys, in case mouse is being odd
	*/
	void Update () {
		if(Input.GetMouseButtonDown(0)){
			rot = t.rotation;

			mouseClickPos = Input.mousePosition;
			mouseDown=true;
		}

		if(mouseDown){
			Vector2 dif = (Vector2)Input.mousePosition-mouseClickPos;

			t.rotation = rot;
			t.Rotate(new Vector3(dif.y/pixPerDeg,-dif.x/pixPerDeg,0),Space.World);
		}

		if(Input.GetMouseButtonUp(0)){
			mouseDown = false;
		}

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
