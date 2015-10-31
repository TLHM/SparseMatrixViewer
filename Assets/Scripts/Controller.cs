using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class Controller : MonoBehaviour {
	public Transform container;
	public Camera cam;
	public Transform nodeFab;
	public Transform edgeFab;
	public Transform megaNodeFab;

	public List<Node> nodes;
	public List<Edge> edges;
	public List<MegaNode> megas;

	public string file;
	public float dt;
	public int colorFactor;
	public int initialPositionStyle;
	public int edgeMode;
	public bool simplify;
	public float nodeFScale;
	public float scrollFactor;
	public float fogDensity;

	public int pauseCount; //How many edges do we process before pausing for the frame?
	public float iLen;	//Our ideal length of edges

	public bool simulating;
	bool upNodes;
	public Gradient gg;

	//For background
	public Color[] bgCols;

	//For the FPS counter
	float acum, timeLeft, avTime;
	int frames;
	public UnityEngine.UI.Text FPS;


	// Initialization
	void Start () {
		Edge.idealLen = iLen;
		Edge.idealLen2 = Edge.idealLen*Edge.idealLen;
		Edge.avLen = iLen;
		Edge.mode = edgeMode;
		Node.forceScale = 2;

		//Create color gradient for the edges
		Edge.g = gg;//new Gradient();
		/*GradientColorKey[] cols = new GradientColorKey[4];
		cols[0].color = Color.red;
		cols[0].time = 0f;
		cols[1].color = Color.yellow;
		cols[1].time = .5f;
		cols[2].color = Color.green;
		cols[2].time = .75f;
		cols[1].color = Color.blue;
		cols[1].time = 1f;

		GradientAlphaKey[] alphs = new GradientAlphaKey[4];
		alphs[0].alpha = 1f;
		alphs[0].time = 0f;
		alphs[1].alpha = .7f;
		alphs[1].time = .5f;
		alphs[2].alpha = .6f;
		alphs[2].time = .75f;
		alphs[1].alpha = .5f;
		alphs[1].time = 1f;
		Edge.g.SetKeys(cols,alphs);
		gg = Edge.g;*/

		StartCoroutine(Load());
	}

	void Update () {
		float scroll = Input.mouseScrollDelta.y*scrollFactor;
		cam.orthographicSize = Mathf.Clamp(cam.orthographicSize-scroll,2.5f,1000f);

		if(Input.GetKeyDown(KeyCode.Space)){
			if(!upNodes){
				upNodes=true;
			}else{
				upNodes=false;
				for(int i=0;i<nodes.Count;i++){
					nodes[i].clearVel();
				}
			}
		}

		if(Input.GetKeyDown(KeyCode.S))
		{
			if(upNodes && simplify)
			{
				simplify = false;
				StartCoroutine(Unsimplify());
			}
		}

		if(simulating){
			Node.forceScale=nodeFScale;
			Edge.colorFactor = colorFactor;
			Shader.SetGlobalFloat("_Density",fogDensity);
			float totalDist=0;
			int relevantEdgeCount=0;
			for(int i=0;i<edges.Count;i++){
				float d=edges[i].UpdateVis();
				if(d!=0)
				{
					relevantEdgeCount++;
					totalDist+=d;
				}
			}
			Edge.avLen = totalDist/relevantEdgeCount;
		}

		timeLeft -= Time.deltaTime;
		acum+=Time.deltaTime;
		frames++;
		if(timeLeft<=0){
			avTime = acum/frames;
			FPS.text = (1000*avTime).ToString("#0.0")+" ms  "+(1.0f/avTime).ToString("#0.0")+"fps\n";
			timeLeft = .5f;
			acum = 0;
			frames = 0;
		}
	}

	IEnumerator Load(){
		//Read in our example file
		string path = Application.dataPath+"/Matrices/"+file+".mtx";
		string data = File.ReadAllText(path);
		//Split it into lines
		string[] lines = data.Split('\n');
		//Begin going through lines
		int i=0;
		//Skip the comments, going straight to the data for now
		while(lines[i].StartsWith("%")){
			i++;
		}
		//Grab the column/row count
		//Square, so we only need one
		int colCount = int.Parse(lines[i].Split(' ')[0]);
		Vector3 posOffset = new Vector3(-.5f,-.5f,0)*colCount;
		int identityCount=0;	//Tracks number of i==i edges

		//Increment to the real data
		i++;
		//Create an array to keep track of the nodes we create, to avoid duplicates
		Node[] n = new Node[colCount];
		//Go through each line
		for(i=i;i<lines.Length;i++){
			//Grab the col,row pair for this value
			// -1 because the file format is 1-based not 0-based
			string[] nums = lines[i].Split(' ');
			//Make sure we have 2 numbers!
			if(nums.Length<2) continue;

			int mi = int.Parse(nums[0])-1;
			int mj = int.Parse(nums[1])-1;

			//If the column and row are the same, ignore it
			if(mi==mj)
			{
				identityCount++;
				continue;
			}

			//Check if both nodes mi and mj exist
			//If not, create the node
			if(n[mi]==null){
				n[mi] = CreateNode(mi, new Vector3(mj,mi,0) - posOffset);
			}
			if(n[mj]==null){
				n[mj] = CreateNode(mj, new Vector3(mj,mi,0) - posOffset);
			}

			//Create an edge between mi and mj
			//This file doesn't contain duplicates, so no worries there
			CreateEdge(n[mi],n[mj]);

			if(i%pauseCount==0) yield return null;
			//if(i>200) break;
		}

		yield return null;

		if(initialPositionStyle==1){
			//Square
			int dim = (int)Mathf.Pow(nodes.Count,1/3f);
			float dx = .5f;
			for(int j=0;j<nodes.Count;j++){
				nodes[j].t.position = new Vector3(-dim*dx*.5f + (j%dim)*dx, -dim*dx*.5f+((j/dim)%dim)*dx,-dim*dx*.5f+((j/(dim*dim))%dim)*dx);
			}
		}else if(initialPositionStyle==2){
			//Cube
			int dim = (int) Mathf.Sqrt(nodes.Count);
			float dx = .5f;
			for(int j=0;j<nodes.Count;j++){
				nodes[j].t.position = new Vector3(-dim*dx*.5f + (j%dim)*dx, -dim*dx*.5f+((j/dim)%dim)*dx,0);
			}
		}else if(initialPositionStyle==3)
		{
			//Sphere
			for(int j=0;j<nodes.Count;j++){
				nodes[j].t.position = Random.onUnitSphere*Random.Range(.25f,1f);
			}
		}else if(initialPositionStyle==0)
		{
			//Center all them nodes
			Vector3 avPos=Vector3.zero;
			for(int j=0;j<nodes.Count;j++)
			{
				avPos+=nodes[j].t.position;
			}
			avPos*=1f/nodes.Count;

			for(int j=0;j<nodes.Count;j++)
			{
				nodes[j].t.position-=avPos;
			}
		}

		Debug.Log("Loaded "+nodes.Count+" nodes and "+edges.Count+" edges\n"+identityCount+" edges ignored");

		if(simplify) yield return StartCoroutine(SimplifyGraph());

		//if(edges.Count>1000) Time.timeScale=.2f;
		simulating=true;
		StartCoroutine(updateNodes());

		yield return new WaitForSeconds(1);

		//if(!upNodes) upNodes=true;

		yield break;
	}

	///Uses the idea of CNG ( http://hcil2.cs.umd.edu/newvarepository/VAST%20Challenge%202012/challenges/MC2%20-%20Bank%20of%20Money%20regional%20Network%20Op/entries/Institute%20of%20Software%20Chinese%20Academy%20of%20Sciences/CNG_report_20120331.pdf)
	///Simplifies clusters of nodes into Mega Nodes, making the physics simulation simpler
	///Later, Mega Nodes will be individually decompressed to get the full graph, if desired
	IEnumerator SimplifyGraph()
	{
		megas = new List<MegaNode>();
		Node n1, n2;
		List<int> addedNodeIDs = new List<int>();
		List<int> addedNodeMegas = new List<int>();

		//O(log2n) loop through all nodes
		//Compare their neighbors, via edges
		//If neighbors are sufficiently similar, they belong together in a mega node
		//If one node already is added to a megaNode, add the other
		//If not, create a new mega node
		//Add their node IDs to the addedNodeIDs and their mega id to addedNodeMegas
		for(int i=0;i<nodes.Count;i++)
		{
			n1=nodes[i];
			for(int j=i+1;j<nodes.Count;j++)
			{
				n2 = nodes[j];
				//Check if we have about same number of neighbors. If not, continue
				if(Mathf.Abs(n1.neighbors.Count - n2.neighbors.Count)<=1)
				{
					bool similar = true;
					for(int k = 0; k<n1.neighbors.Count; k++)
					{
						//If one node has a neighbor that the other node doesn't have, and it isn't the other node
						//They don't belong together!!
						if( (n1.neighbors[k]!=n2.index && !n2.neighbors.Contains(n1.neighbors[k])) ||
						(n2.neighbors[k]!=n1.index && !n1.neighbors.Contains(n2.neighbors[k])) )
						{
							similar = false;
							break;
						}

						//If these two nodes belong together, let's get em in a mega node
						if(similar)
						{
							//Check if either node already is in a mega node
							if(addedNodeIDs.Contains(n1.index))
							{
								int index = addedNodeIDs.IndexOf(n1.index);
								index = addedNodeMegas[index];

								//Add to the correct Mega Node
								megas[index].AddNode(n2);
								addedNodeIDs.Add(n2.index);
								addedNodeMegas.Add(index);
							}else if (addedNodeIDs.Contains(n2.index))
							{
								int index = addedNodeIDs.IndexOf(n2.index);
								index = addedNodeMegas[index];

								//Add to the correct mega node
								megas[index].AddNode(n1);
								addedNodeIDs.Add(n1.index);
								addedNodeMegas.Add(index);
							}else{
								//Neither already in a mega node, make a new one
								int megaId = megas.Count;
								megas.Add(CreateMegaNode(megaId));

								//Add the nodes to the mega
								megas[megaId].AddNode(n1);
								addedNodeIDs.Add(n1.index);
								addedNodeMegas.Add(megaId);

								megas[megaId].AddNode(n2);
								addedNodeIDs.Add(n2.index);
								addedNodeMegas.Add(megaId);

								//Set the mega node position
								megas[megaId].t.localPosition = (n1.t.localPosition + n2.t.localPosition)/2f;
							}
						}
					}
				}
			}
		}

		//Loop through all MegaNodes
		//Set their edges, and save them in our edges list
		//set them to simulating (and disable their constituents)
		for(int i=0; i<megas.Count; i++)
		{
			//Which nodes to create edges with?
			int[] edgesToAdd = megas[i].AddEdges();
			//Add edges
			for(int j=0;j<edgesToAdd.Length;j++)
			{
				Node nn = nodes[edgesToAdd[j]];
				if(nn.simulating)
				{
					CreateEdge( megas[i], nn );
				}else
				{
					CreateEdge( megas[i], megas[nn.megaID] );
				}

			}
			//Set up simulating mega node
			megas[i].BeginSim();
		}
		Debug.Log("Added "+megas.Count+" MegaNodes");
		yield break;
	}


	///As the name implies, updates the nodes over several frames, if needed.
	///Edges are updated each frame in Update()
	IEnumerator updateNodes(){
		while(simulating){
			if(!upNodes){
				yield return null;
				continue;
			}
			Node n;
			int count = 0;
			int frameLimit = 50000;
			for(int i=0;i<nodes.Count;i++){
				n=nodes[i];
				if(!n.simulating) continue;

				Vector3 repulse;
				//Get forces from other nodes
				for(int j=i+1;j<nodes.Count;j++){
					if(!nodes[j].simulating) continue;

					repulse = n.CalcRepulse(nodes[j]);
					n.force+=repulse;
					nodes[j].force-=repulse;
					count++;

					if(count>frameLimit){
						count=0;
						yield return null;
					}
				}
				//Get forces from megas
				for(int j=i+1;j<megas.Count;j++){
					if(!megas[j].simulating) continue;

					repulse = n.CalcRepulse(megas[j]);
					n.force+=repulse;
					megas[j].force-=repulse;
					count++;

					if(count>frameLimit){
						count=0;
						yield return null;
					}
				}
				//Debug.Log("Repulsive for for node"+n.id+" is "+n.force);
				Vector3 edgeF;
				for(int j=0;j<n.edges.Count;j++){
					//Should also apply edge force to both here - future improvement
					edgeF = n.edges[j].GetForce(n);
					//if(edgeF.sqrMagnitude>10000) Debug.Log("wutE "+edgeF);
					n.force+=edgeF;
					//Debug.Log("Edge force for node"+n.id+":"+edgeF);

					count++;

					if(count>frameLimit){
						count=0;
						yield return null;
					}
				}
				n.ApplyForce(dt);
			}

			//Process Mega Nodes Edges and inter repulsion
			for(int i=0; i<megas.Count; i++)
			{
				n=megas[i];
				if(!n.simulating) continue;

				Vector3 repulse;
				for(int j=i+1;j<megas.Count;j++){
					if(!megas[j].simulating) continue;

					repulse = n.CalcRepulse(megas[j]);
					n.force+=repulse;
					megas[j].force-=repulse;
					count++;

					if(count>frameLimit){
						count=0;
						yield return null;
					}
				}
				//Debug.Log("Repulsive for for node"+n.id+" is "+n.force);
				Vector3 edgeF;
				for(int j=0;j<n.edges.Count;j++){
					//Should also apply edge force to both here - future improvement
					edgeF = n.edges[j].GetForce(n);
					//if(edgeF.sqrMagnitude>10000) Debug.Log("wutE "+edgeF);
					n.force+=edgeF;
					//Debug.Log("Edge force for node"+n.id+":"+edgeF);

					count++;

					if(count>frameLimit){
						count=0;
						yield return null;
					}
				}
				n.ApplyForce(dt);
			}

			for(int i=0;i<nodes.Count;i++){
				nodes[i].UpdatePos();
			}
			for(int i=0;i<megas.Count;i++){
				megas[i].UpdatePos();
			}

			yield return null;
		}
	}

	/**Unsimplifies graph
	just calls StopSim() on mega nodes over time
	*/
	IEnumerator Unsimplify(){
		for(int i=0;i<megas.Count;i++)
		{
			megas[i].StopSim();
			if(i%3==0) yield return new WaitForSeconds(.1f);
		}
		yield break;
	}

	void CreateEdge(Node n1, Node n2){
		Edge e = (Instantiate(edgeFab) as Transform).GetComponent<Edge>();
		e.n1=n1;
		e.n2=n2;
		n1.AddEdge(e);
		n2.AddEdge(e);
		e.name="edge"+n1.id+"-"+n2.id;
		e.t.SetParent(container);
		edges.Add(e);
	}

	//id is the row/col number from the file
	//Pos also determined on location in the matrix
	Node CreateNode(int id, Vector2 pos){
		Node n = (Instantiate(nodeFab) as Transform).GetComponent<Node>();
		n.t.position = pos*.005f;//Random.onUnitSphere*Random.Range(.25f,1f);//Vector3.right*(id%24)*.1f+Vector3.up*(4-id/24)*.1f;
		n.id = id;
		n.mass=1;
		n.index = nodes.Count;
		nodes.Add(n);
		n.t.SetParent(container);
		n.name="node"+id;
		return n;
	}

	//Passed in id is the index in the mega node collection
	MegaNode CreateMegaNode(int id)
	{
		MegaNode n = (Instantiate(megaNodeFab) as Transform).GetComponent<MegaNode>();
		n.t.position = Random.onUnitSphere*Random.Range(.25f,1f);
		n.id = -id;
		n.index = id;
		n.mass=0;

		n.t.SetParent(container);
		n.name="megaNode"+id;

		return n;
	}
}
