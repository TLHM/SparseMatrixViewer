using UnityEngine;
using System.Collections;

/**
	Represents an edge between two nodes in our graph
	Can return the force it applies on its nodes
*/
public class Edge : MonoBehaviour {
	public static float idealLen;		/**< Ideal length for the edges*/
	public static float idealLen2; 	/**< Ideal length for the edges squared*/
	public static Gradient g;			/**< Gradient that colors our edges */
	public static float colorFactor;	/**< Shifts the scaling of how our gradient is mapped to edge lengths */
	public static float avLen;			/**< Last calculated average edge length. Set in Controller. */

	public Node n1;	/**< First node that comprises this edge */
	public Node n2;	/**< Second node that comprises this edge */

	public Transform t;			/**< The transform of this edge (at the midpoint) */
	public LineRenderer lr;		/**< The visual line renderer for this edge. */
	Vector3 dir;					/**< The vector from node2 to node1 */
	Color curColor;				/**< The current color of the edge, as calculated in UpdateVis() */

	/**
		Returns a Vector3 that represents the force applied on a node
		@param n Which node we want the force for. If it doesn't belong to this edge, a zero force is returned
	*/
	public Vector3 GetForce(Node n){
		if(!n1.simulating || !n2.simulating) return Vector3.zero;

		bool node1;
		if(n==n1) node1=true;
		else if(n==n2) node1=false;
		else return Vector3.zero;
		dir = n1.t.localPosition - n2.t.localPosition;
		//Debug.Log("EForce mag for node"+n.id+" ("+node1+") is "+(dir.sqrMagnitude/idealLen)+dir.normalized);
		//if(dir.sqrMagnitude<idealLen) return (dir.normalized*(-dir.sqrMagnitude/idealLen))*(node1?-1:1);
		float mag = Mathf.Min((dir.sqrMagnitude/idealLen),200);
		return (dir.normalized*mag)*(node1?-1:1);
	}

	/**Updates the visual position, length, rotation, and color of the edge
		if either node of the edge is not simulating, the edge is moved far away and hidden
	*/
	public float UpdateVis(){
		//Are we actually relevant right now?
		if(!n1.simulating || !n2.simulating)
		{
			//No, run away and hide!
			t.localPosition = Vector3.right*99999;
			lr.enabled = false;
			return 0;
		}else
		{
			//Yes, make sure we're visible
			lr.enabled = true;
		}

		//Update the line renderer visual
		dir = n1.t.localPosition - n2.t.localPosition;
		float mag = dir.magnitude;
		t.localPosition = n2.t.localPosition;
		lr.SetPosition(0,Vector3.zero);
		lr.SetPosition(1,dir);

		curColor = g.Evaluate(mag/(avLen*colorFactor));
		lr.SetColors(curColor,curColor);

		return mag;
	}

	/**
		Returns the current color of the edge in a hex string
		@return Hex color of the edge in string format
	*/
	public string GetHexColor()
	{
		string hex = "#";
		hex += IntToHex(Mathf.RoundToInt(curColor.r*255));
		hex += IntToHex(Mathf.RoundToInt(curColor.g*255));
		hex += IntToHex(Mathf.RoundToInt(curColor.b*255));

		return hex;
	}

	/**
		Returns an integer from 0-255 as a 2 digit hex string
		@param i An integer between 0 and 255 (inclusive)
	*/
	string IntToHex(int i)
	{
		int tens = i/16;
		int hex = i%16;

		return HexDigit(tens)+HexDigit(hex);
	}

	/**
		Converts a int from 0-15 to a hex string character
		Defaults to "0" if the integer is out of bounds
		@param i Integer between 0-15 (inclusive)
	*/
	string HexDigit(int i)
	{
		switch(i){
			case 0:
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 8:
			case 9:
				return i.ToString();
				break;
			case 10:
				return "a";
				break;
			case 11:
				return "b";
				break;
			case 12:
				return "c";
				break;
			case 13:
				return "d";
				break;
			case 14:
				return "e";
				break;
			case 15:
				return "f";
				break;
		}

		return "0";
	}

	/*Not working as hoped. Don't use. Kept around to remember the shameee.
	public Vector3 GetEulerAngles(Vector3 v){
		return new Vector3(0,Mathf.Atan2(-v.z,v.x)*Mathf.Rad2Deg,-Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg);
	}*/

	/**
		Calculates the correct rotation for the edge, given the direction of the edge
		Given a directional vector for the x direction
		Returns a Vector3 for local Euler, via Quaternion.LookRotation

		Currently unused, since LineRenderers don't need it.

		@param v Vector defining the direction of the edge (n1.pos-n2.pos)
	*/
	public Vector3 GetEulerAnglesQ(Vector3 v){
		Vector3 cross = Vector3.Cross(v,Vector3.up);
		if(cross==Vector3.zero) return Vector3.forward;

		Quaternion q = Quaternion.LookRotation(cross,Vector3.Cross(cross,v));
		return q.eulerAngles;
	}

}
