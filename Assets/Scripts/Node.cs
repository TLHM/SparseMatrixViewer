using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
	Node is the basic graph node. It keeps track of edges and neighbors.
	Has some simple physics calculations
*/
public class Node : MonoBehaviour {
	public static float forceScale;	/**< Gets set by Controller. A multiplier on force */
	public int id;			/**< Identifier according to input file row number */
	public int index; 	/**< index in Controller's list of nodes */
	public int megaID;	/**< If added to a mega, it's the index in Controller.megas of the mega */
	public float mass;	/**< Should always be 1 for non-mega nodes. For force calculations */
	public List<Edge> edges;		/**< List of all edges connected to this node */
	public List<int> neighbors;	/**< Node indexes of nodes connected through edges */

	public Transform t;	/**< The transform of the node */

	public bool simulating;			/**< If false, it won't be updated. When part of an active mega node, will be false. */

	//Used for the physics simulation
	public Vector3 force;	/**< Contains the force that a node is affectd by each update tick */
	Vector3 velocity;			/**< Current velocity of the node */
	Vector3 last;				/**< Previous velocity. Used to detect and reduce degenerate behavior with larger time steps */

	//Used to estimate slowing down time, ending simulation
	Vector3 history;		/**< A previous position. Used to detect when the simulation is approachign a stable solution */

	/**
		Calculates a repulsion force from another node
		@param n The node we're recieving a repulsive force from.
	*/
	public Vector3 CalcRepulse(Node n){
		Vector3 dir = t.localPosition-n.t.localPosition;
		float mag = dir.magnitude;

		//In case we're right on top of one another
		if(mag==0) return .5f*Random.onUnitSphere;

		mag = Mathf.Max(mag,.05f);
		return dir.normalized * (Edge.idealLen2/mag);
	}

	/**
		Applies the previously calculated force to self, resets it for the next frame
		Contains some checks to stop degenerate behavior
		@param dt The time step we are simulating (delta time).
	*/
	public void ApplyForce(float dt){
		float sqrMag = force.sqrMagnitude;
		if(sqrMag<.01f){
			force.Set(0,0,0);
		}
		//Makes sure the force is not too large
		if(sqrMag>200)
		{
			force*=.1f;
		}

		//Check if we're flip flopping
		if((force.normalized+last.normalized).sqrMagnitude<.01f)
		{
			velocity.Set(0,0,0);
			force*=.5f;
		}

		velocity+=(force/mass)*forceScale*dt;
		last = force;
		force.Set(0,0,0);
	}

	/**
		Just adds an edge to our list. Edges are created and initialized in Controller.
		Adds the neighbor the edge connects to.
		@param e The edge to be added.
	*/
	public void AddEdge(Edge e){
		edges.Add(e);
		if(e.n1.id!=id) neighbors.Add(e.n1.index);
		else neighbors.Add(e.n2.index);
	}

	/**
		Actually moves the node using the velocity calculated with ApplyForce
		Lowers velocity as well. This helps reduce degenerate behavior,
		though it means the simulation takes longer.
		@return Returns a Vector2. X value is the number of edges updated, Y value is their summed lengths.
	*/
	public Vector2 UpdatePos(){
		t.localPosition+=velocity*Time.deltaTime;
		velocity*=.5f;

		int edgeCount=0;
		float totalDist=0;
		for(int i=0;i<edges.Count;i++)
		{
			float d=edges[i].UpdateVis();
			if(d!=0)
			{
				edgeCount++;
				totalDist+=d;
			}
		}

		return new Vector2(edgeCount,totalDist);
	}

	/**
		Sets the speed to zero
	*/
	public void clearVel(){
		velocity.Set(0,0,0);
	}

	/**Saves current local position as history */
	public void recordHistory(){
		history = t.localPosition;
	}

	/**Returns the difference between history position and current posisiton (local) */
	public Vector3 difFromHistory(){
		return t.localPosition - history;
	}
}
