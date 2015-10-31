using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/** Mega Node class is a grouping of nodes, for simpler physics simulation
*/
public class MegaNode : Node
{
	public List<Node> containedNodes;
	public List<Vector3> relativePos;
	public List<int> containedIDs;

	void init()
	{
		containedNodes = new List<Node>();
		relativePos = new List<Vector3>();
	}

	/// Sets this MegaNode to simulating, and stops simulating all contained nodes
	public void BeginSim()
	{
		simulating = true;
		for(int i=0;i<containedNodes.Count;i++)
		{
			containedNodes[i].simulating = false;
			relativePos[i] = containedNodes[i].t.localPosition - t.localPosition;
		}
	}

	/// Sets this MegaNode to not simulating, and begins simulating all contained nodes
	public void StopSim()
	{
		simulating = false;
		for(int i=0;i<containedNodes.Count;i++)
		{
			containedNodes[i].simulating = true;
			containedNodes[i].t.localPosition = t.localPosition + relativePos[i];
		}
	}

	///Adds a new node to the mega node
	public void AddNode(Node n)
	{
		//Modify position
		if(containedNodes.Count>1){
			Vector3 avPos = t.localPosition*containedNodes.Count + n.t.localPosition;
			t.localPosition = avPos/(containedNodes.Count+1);
		}

		containedNodes.Add(n);
		relativePos.Add(n.t.localPosition - t.localPosition);
		mass++;
		containedIDs.Add(n.id);
		n.simulating = false;
		n.megaID = index;


	}

	///Called after all nodes that will be added are added
	///Searches for edges to nodes outside of the mega node, and adds them to it
	public int[] AddEdges()
	{
		//Check which neighbors of contained nodes aren't contained themselves
		List<int> edgesNeeded = new List<int>();
		for(int i=0; i<containedNodes.Count; i++)
		{
			for(int j=0; j<containedNodes[i].neighbors.Count; j++)
			{
				if(!containedIDs.Contains(containedNodes[i].neighbors[j]))
				{
					edgesNeeded.Add(containedNodes[i].neighbors[j]);
				}
			}
		}

		//Return which nodes we need edges to. Controller will add them
		return edgesNeeded.ToArray();
	}

	/** Returns a bool of whether or not this mega node contains a node with id matching the param
	*/
	public bool containsNode(int id){
		return containedIDs.Contains(id);
	}
}
