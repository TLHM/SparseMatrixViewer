using UnityEngine;
using System.Collections;

/**Represents an edge between two nodes in our graph
	Can return the force it applies on its nodes
*/
public class Edge : MonoBehaviour {
	public static float idealLen;		/**<Ideal length for the edges*/
	public static float idealLen2; 	/**<Ideal length for the edges squared*/
	public static Gradient g;
	public static int colorFactor;
	public static float avLen;
	public static Material edgeMat;

	public Node n1;
	public Node n2;

	public Transform t;
	public LineRenderer lr;
	Material m;
	Vector3 dir;

	/**Returns a V3 that represents the force applied on a node
	//If node1 is true, returns force on node 1
	//Otherwise, returns the force for node 2 */
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
		if(!n1.simulating || !n2.simulating)
		{
			t.localPosition = Vector3.right*99999;
			lr.enabled = false;
			return 0;
		}else
		{
			lr.enabled = true;
		}

		dir = n1.t.localPosition - n2.t.localPosition;
		float mag = dir.magnitude;
		lr.SetPosition(0,n1.t.position);
		lr.SetPosition(1,n2.t.position);

		Color col = g.Evaluate(mag/(avLen*colorFactor));
		lr.SetColors(col,col);

		return mag;
	}

	/*Not working as hoped. Don't use
	public Vector3 GetEulerAngles(Vector3 v){
		return new Vector3(0,Mathf.Atan2(-v.z,v.x)*Mathf.Rad2Deg,-Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg);
	}*/

	/**Attempt at getting correct rotation via quaternions
		Attempt was successful. Given a directional vector for the x direction
		Returns a Vector3 for local Euler, via Quaternion
	*/
	public Vector3 GetEulerAnglesQ(Vector3 v){
		Vector3 cross = Vector3.Cross(v,Vector3.up);
		if(cross==Vector3.zero) return Vector3.forward;

		Quaternion q = Quaternion.LookRotation(cross,Vector3.Cross(cross,v));
		return q.eulerAngles;
	}

}
