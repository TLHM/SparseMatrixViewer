using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Node : MonoBehaviour {
	public static float forceScale;
	public int id;		//Identifier according to input file row number
	public int index; //index in Controller's list of nodes
	public int megaID;	//If added to a mega, it's the index in Controller.megas of the mega
	public float mass;
	public List<Edge> edges;	//List of all nodes attached to this one
	public List<int> neighbors;	//node ids of nodes connected through edges

	public Transform t;

	public bool simulating;

	//Used for the physics simulation
	public Vector3 force;
	Vector3 velocity;
	Vector3 last;

	//Calculates a repulsion force from another node
	public Vector3 CalcRepulse(Node n){
		Vector3 dir = t.localPosition-n.t.localPosition;
		float mag = dir.magnitude;

		if(mag==0) return .5f*Random.onUnitSphere;
		mag = Mathf.Max(mag,.05f);
		return dir.normalized * (Edge.idealLen2/mag);
	}

	//Applies the force to self, resets it for the next frame
	public void ApplyForce(float dt){
		float sqrMag = force.sqrMagnitude;
		if(sqrMag<.01f){
			force.Set(0,0,0);
		}
		//Makes sure the force not too large
		if(sqrMag>200)
		{
			force*=.1f;
		}

		//Check if we're flip flopping
		if((force.normalized+last.normalized).sqrMagnitude<.01f)
		{
			//Debug.Log("asdasd");
			velocity.Set(0,0,0);
			force*=.5f;
		}

		velocity+=(force/mass)*forceScale*dt;
		last = force;
		force.Set(0,0,0);
	}

	//Just adds an edge to our list
	public void AddEdge(Edge e){
		edges.Add(e);
		if(e.n1.id!=id) neighbors.Add(e.n1.index);
		else neighbors.Add(e.n2.index);
	}

	public void UpdatePos(){
		t.localPosition+=velocity*Time.deltaTime;
		velocity*=.5f;
	}

	public void clearVel(){
		velocity.Set(0,0,0);
	}
}
