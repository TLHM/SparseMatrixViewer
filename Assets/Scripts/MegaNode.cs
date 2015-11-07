using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
	Mega Node class is a grouping of nodes, for simplifying physics simulation
*/
public class MegaNode : Node
{
	public List<Node> containedNodes;	/**< A list of the nodes that merged to form this mega node */
	public List<Vector3> relativePos;	/**< The relative positions of the nodes when they were merged */
	public List<int> containedIDs;		/**< The id */

	/**
		Creates empty lists
	*/
	void init()
	{
		containedNodes = new List<Node>();
		relativePos = new List<Vector3>();
	}

	/**
		Sets this MegaNode to simulating, and stops simulating all contained nodes
	*/
	public void BeginSim()
	{
		simulating = true;
		for(int i=0;i<containedNodes.Count;i++)
		{
			containedNodes[i].simulating = false;
			relativePos[i] = containedNodes[i].t.localPosition - t.localPosition;
		}
	}

	/**
		Sets this MegaNode to not simulating, and begins simulating all contained nodes
	*/
	public void StopSim()
	{
		simulating = false;
		for(int i=0;i<containedNodes.Count;i++)
		{
			containedNodes[i].simulating = true;
			containedNodes[i].t.localPosition = t.localPosition + relativePos[i];
		}
	}

	/**
		Adds a new node to the mega node
		Mass is increased, and the node is added to containedNodes.
		Relative position is also saved, but it's likely to be wonky, not that it matters too much.
		The new node is also disabled (simulating = false), and gets an index pointer to this mega node.
		@param n The node to be added
	*/
	public void AddNode(Node n)
	{
		//Modify position
		//Mega node aims to have the average position of its contained nodes
		Vector3 avPos = t.localPosition*containedNodes.Count + n.t.localPosition;
		t.localPosition = avPos/(containedNodes.Count+1);

		containedNodes.Add(n);
		relativePos.Add(n.t.localPosition - t.localPosition);

		mass++;
		mass = Mathf.Min(mass, 3);	//Cap mass, should make the mega sims more stable
		containedIDs.Add(n.id);
		n.simulating = false;
		n.megaID = index;
	}

	/**
		Called after all nodes that will be added are added
		Searches for edges to nodes outside of the mega node
		Returns a list of indexes of nodes the mega needs edges to
	*/
	public int[] AddEdges()
	{
		//Check which neighbors of contained nodes aren't contained themselves
		List<int> edgesNeeded = new List<int>();
		for(int i=0; i<containedNodes.Count; i++)
		{
			for(int j=0; j<containedNodes[i].neighbors.Count; j++)
			{
				int edgeCheck = containedNodes[i].neighbors[j];
				if(!containedIDs.Contains(edgeCheck) && !edgesNeeded.Contains(edgeCheck))
				{
					edgesNeeded.Add(edgeCheck);
				}
			}
		}

		//Return which nodes we need edges to. Controller will add them
		return edgesNeeded.ToArray();
	}

	/**
		Returns a bool of whether or not this mega node contains a node with id matching the param
		@param id The node index to be checked for
	*/
	public bool containsNode(int id){
		return containedIDs.Contains(id);
	}
}
