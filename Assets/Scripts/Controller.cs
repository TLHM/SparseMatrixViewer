using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/**
	Controller is essentailly the main function of the program. It loads data and updates the
	simulation.

*/
public class Controller : MonoBehaviour {
	public Transform container;	/**< Transform that houses our graph*/
	public Camera cam;				/**< The main camera */
	public Transform nodeFab;		/**< Transform that is the generic node */
	public Transform edgeFab;		/**< Transform that is the generic edge */
	public Transform megaNodeFab;	/**< Transform that is the generic mega node */

	public List<Node> nodes;		/**< Keeps track of all our normal nodes */
	public List<Edge> edges;		/**< Keeps track of all our edges */
	public List<MegaNode> megas;	/**< Keeps track of all our mega nodes */

	/**
		File name of the matrix file we want to Load.
		Should NOT include the file ending (.mtx / .mtxs)
		For .mtx files, file name will be relative to Assets/Matrices
		For .mtxs files, file name will be relative to Assets/SolvedMatrices
		For a .mtxs file, the boolean solved should be true.
		For normal .mtx files, solved should be false.
		EG "qh882" or "sub2k/can_229"
	*/
	public string file;
	public bool solved;				/**< Are we loading a solved .mtxs (true) or a .mtx (false)? */
	public float dt;					/**< Delta Time variable. Should being at around 1, and will automatically be reduced as the simulation continues. */
	public float colorFactor;		/**< Multiplier that affects how the colors are mapped to the gradient. Recommended value of 2 */
	public int initialPositionStyle;	/**< Defines distrubution of the nodes when first created. 0 = position in matrix, 1 = square, 2 = cube, 3 = randomized sphere */
	public bool simplify;			/**< If true, the graph is simplified before beginning simulation. It will unsimplify during the simulation */
	public float nodeFScale;		/**< Value that determines Node.forceScale during simulation. You can change it through the inspector during a simulation */
	public float scrollFactor;		/**< Value that determines how quickly you zoom in and out using the mouse scroll. */
	public float fogDensity;		/**< Value controls how the fog that affects objects further away. */

	public int pauseCount; 			/**< How many edges do we process before pausing for the frame? */
	public float iLen;				/**< Our ideal length of edges. Should be set before running the scene, rather than during. */

	public bool simulating;			/**< Are we updating edges? Should almost always be true. */
	public bool upNodes;				/**< Are we simulating physics on nodes right now? */
	public Gradient gg;				/**< Gradient that determines color of edges, based on length and the colorFactor. */

	//For background
	public Color[] bgCols;			/**< Should hold two colors, to determine the clear color [0], background cloud color [1], and fog color [1] Set before runtime*/
	public Material bgMat;			/**< Material pointer for background clouds */
	public Material edgeMat;		/**< Material pointer for edges */

	//For the FPS counter
	float acum, timeLeft, avTime;
	int frames;
	public UnityEngine.UI.Text FPS;

	//Loading GUI
	public UnityEngine.UI.Text loadingMessage;
	public RectTransform loadingBar;

	/**
		For slowing down the simulation, and ending it
		The frame count is for ticks of the updateNodes coroutine
	*/
	public int framesUntilCheck;
	int framesPerCheck;

	// Initialization
	void Start () {
		//Initialize length variables for Edges
		Edge.idealLen = iLen;
		Edge.idealLen2 = Edge.idealLen*Edge.idealLen;
		Edge.avLen = iLen;
		//Initialize the forceScale for nodes
		Node.forceScale = 2;

		//Create color gradient for the edges
		//Commented out gradient was creted in code
		//Currently, you should just set it using the inspector in the Unity Editor
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

	/** Processes some input
		Updates edges if simulating
		Sets some static variables (Force Scale, Color Factor, Fog Density)
		Calculate and displays Framerate
	*/
	void Update () {
		//Zoom the camera in/out
		float scroll = Input.mouseScrollDelta.y*scrollFactor;
		//cam.fieldOfView = Mathf.Clamp(cam.fieldOfView-scroll,2.5f,1000f);
		cam.orthographicSize = Mathf.Clamp(cam.orthographicSize-scroll,2.5f,88f);

		//Pause and resume updating of nodes (simulating physics on them)
		if(Input.GetKeyDown(KeyCode.Space) && simulating){
			if(!upNodes){
				upNodes=true;
			}else{
				upNodes=false;
				for(int i=0;i<nodes.Count;i++){
					nodes[i].clearVel();
				}
			}
		}

		//Can manually unsimplify. Not recommended
		if(Input.GetKeyDown(KeyCode.S))
		{
			if(upNodes && simplify)
			{
				simplify = false;
				StartCoroutine(Unsimplify());
			}
		}

		if(simulating){
			//Set staic variables, so we can modify them through the inspector while running
			Node.forceScale=nodeFScale;
			Edge.colorFactor = colorFactor;
			Shader.SetGlobalFloat("_Density",fogDensity);

			//Update edges, and calculate the average edge length (which dictates coloring)
			//Moved to be a part of updateNodes
			/*float totalDist=0;
			int relevantEdgeCount=0;
			for(int i=0;i<edges.Count;i++){
				float d=edges[i].UpdateVis();
				if(d!=0)
				{
					relevantEdgeCount++;
					totalDist+=d;
				}
			}
			Edge.avLen = totalDist/relevantEdgeCount;*/
		}

		//Calculates & displays the framerate and average frame time
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

	/**
		Loads in the data from our .mtx file (see "file" variable)
		Creates nodes and edges according to the data, and arranges them according to initialPositionStyle
		Simplifies the graph is simplify is true
		Begins the simulation
	*/
	IEnumerator Load(){
		//Set some BG color stuff before we begin
		cam.backgroundColor = bgCols[0];
		bgMat.SetColor("_Color",bgCols[1]);
		edgeMat.SetColor("_FogColor", bgCols[1]);

		loadingMessage.text = "Reading File";

		if(!solved)		//Loading a raw .mtx file, will have to solve it!
		{

			//Read in our data file
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

			Debug.Log("Beginning read. Column count is "+colCount);

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
				//This file should't contain duplicates, so no check
				CreateEdge(n[mi],n[mj]);

				if(i%pauseCount==0)
				{
					loadingBar.localScale = new Vector3(i/(lines.Length+0f),1,1);
					yield return null;
				}
				//if(i>200) break;
			}

			yield return null;

			//Arrange our nodes if relevant
			//Are by default in positions corresponding to where they were first encountered in the matrix
			if(initialPositionStyle==1){
				//Cube - Like square, but 3D
				int dim = (int)Mathf.Pow(nodes.Count,1/3f);
				float dx = .5f;
				for(int j=0;j<nodes.Count;j++){
					nodes[j].t.position = new Vector3(-dim*dx*1f + (j%dim)*dx, -dim*dx*1f+((j/dim)%dim)*dx,-dim*dx*1f+((j/(dim*dim))%dim)*dx);
				}
			}else if(initialPositionStyle==2){
				//Square - 2D according to order of node creation
				int dim = (int) Mathf.Sqrt(nodes.Count);
				float dx = .5f;
				for(int j=0;j<nodes.Count;j++){
					nodes[j].t.position = new Vector3(-dim*dx*1f + (j%dim)*dx, -dim*dx*1f+((j/dim)%dim)*dx,0);
				}
			}else if(initialPositionStyle==3)
			{
				//Sphere	- randomized positions in a sphereical shape
				int dim = (int)Mathf.Pow(nodes.Count,1/3f);
				for(int j=0;j<nodes.Count;j++){
					nodes[j].t.position = Random.onUnitSphere*Random.Range(.25f,1f+dim);
				}
			}else if(initialPositionStyle==0)
			{
				//Default - Matrix positions
				//Center the nodes in the screen
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

			Debug.Log("Loaded "+nodes.Count+" nodes and "+edges.Count+" edges\n"+identityCount+" edges ignored (edges to self)");

			//If we're simplifing, do it here
			if(simplify) yield return StartCoroutine(SimplifyGraph());

			//Prepare to simulate
			simulating=true;
			framesUntilCheck = 50;
			framesPerCheck = 50;
			StartCoroutine(updateNodes());

			loadingMessage.gameObject.SetActive(false);

			yield return new WaitForSeconds(.5f);

			//Flip the bool if we haven't and begin the actual simulating
			if(!upNodes) upNodes=true;
		}
		else
		{
			//If we're loading an already solved file, do things a bit differently!
			//Read in our data file
			string path = Application.dataPath+"/SolvedMatrices/"+file+".mtxs";
			string data = File.ReadAllText(path);
			//Split it into lines
			string[] lines = data.Split('\n');
			int count = 0;

			//Grab the node and edge count
			string[] lineOne = lines[0].Split(' ');
			int nodeCount = int.Parse(lineOne[0]);
			int edgeCount = int.Parse(lineOne[1]);
			count++;

			loadingMessage.text = "Loading nodes.";
			yield return null;

			//Load in the Nodes
			for(int i=count;i<count+nodeCount;i++)
			{
				string[] nums = lines[i].Split(' ');
				int index = int.Parse(nums[0]);
				int ID = int.Parse(nums[1]);
				float posx = float.Parse(nums[2]);
				float posy = float.Parse(nums[3]);
				float posz = float.Parse(nums[4]);

				Node n = CreateNode(ID,new Vector3(posx,posy,poz));

				if((count+i)%pauseCount==0)
				{
					loadingBar.localScale = new Vector3(i/(nodeCount+0f),1,1);
					yield return null;
				}
			}

			loadingMessage.text = "Loading edges.";
			yield return null;

			count+=nodeCount;

			//Load in the Edges
			for(int i=count;i<=count+edgeCount;i++)
			{
				string[] nums = lines[i].Split(' ');
				int node1 = int.Parse(nums[0]);
				int node2 = int.Parse(nums[1]);

				CreatEdge(nodes[node1],nodes[node2]);

				if((count+i)%pauseCount==0)
				{
					loadingBar.localScale = new Vector3(i/(edgeCount+0f),1,1);
					yield return null;
				}
			}

			loadingMessage.text = "Updating Edges.";

			//Update all the edges twice to calculate their colors and positions.
			for(int j=0;j<2;j++)
			{
				float sumEdges=0;
				int numEdges = 0;
				for(int i=0;i<edges.Count;i++)
				{
					float d = edges[i].UpdateVis();
					if(d>0)
					{
						numEdges++;
						sumEdges+=d;
					}

					if(i%pauseCount==0)
					{
						loadingBar.localScale = new Vector3(i/(edgeCount+0f),1,1);
						yield return null;
					}
				}

				Edges.avLen = sumEdges/numEdges;
			}

			loadingMessage.gameObject.SetActive(false);
		}


		yield break;
	}

	/**
		Uses the idea of CNG ( http://hcil2.cs.umd.edu/newvarepository/VAST%20Challenge%202012/challenges/MC2%20-%20Bank%20of%20Money%20regional%20Network%20Op/entries/Institute%20of%20Software%20Chinese%20Academy%20of%20Sciences/CNG_report_20120331.pdf)
		Simplifies clusters of nodes into Mega Nodes, making the physics simulation simpler
		Later, Mega Nodes will be decompressed to get the full graph
	*/
	IEnumerator SimplifyGraph()
	{
		loadingMessage.text = "Calculating Mega Nodes";

		megas = new List<MegaNode>();
		Node n1, n2;
		List<int> addedNodeIDs = new List<int>();
		List<int> addedNodeMegas = new List<int>();
		int calcCount=0;

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
						( k<n2.neighbors.Count && (n2.neighbors[k]!=n1.index && !n1.neighbors.Contains(n2.neighbors[k])) ) )
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
				calcCount++;
				if(calcCount%20000==0)
				{
					loadingBar.localScale = new Vector3(i/(nodes.Count+0f),1,1);
					yield return null;
				}
			}
		}

		Debug.Log("Added "+megas.Count+" MegaNodes");
		loadingMessage.text = "Creating Mega Node Edges";
		yield return null;

		/*
			Loop through all MegaNodes
			Set their edges, and save them in our edges list
			set them to simulating (and disable their constituents)
		*/
		for(int i=0; i<megas.Count; i++)
		{
			//Which nodes to create edges with?
			int[] edgesToAdd = megas[i].AddEdges();
			//Add edges
			for(int j=0;j<edgesToAdd.Length;j++)
			{
				Node nn = nodes[edgesToAdd[j]];
				//If nn is not part of a mega node
				if(nn.simulating)
				{
					CreateEdge( megas[i], nn );
				}else
				{
					//Check if we've already linked to that mega or not
					if(!megas[i].neighbors.Contains(nn.megaID))
					{
						CreateEdge( megas[i], megas[nn.megaID] );
					}
				}
				if(j%4000==0){
					loadingBar.localScale = new Vector3(i/(megas.Count+0f),1,1);
					yield return null;
				}
			}
			//Set up simulating mega node
			megas[i].BeginSim();
		}
		Debug.Log("Added MegaNode edges. Simplification complete.");
		yield break;
	}


	/**
		As the name implies, updates the nodes with a simple physics simulation
		Takes several frames, if needed.
		Edges are updated each real frame in Update()
	*/
	IEnumerator updateNodes(){
		while(simulating){
			//If we shouldn't be updating nodes, keep spinning
			if(!upNodes){
				yield return null;
				continue;
			}

			//Prep some variables
			Node n;
			int count = 0;	//How many nodes we updated
			int frameLimit = 50000;	//How many nodes we update before we should wait for the next frame

			framesUntilCheck--;

			float totalDist=0;
			int relevantEdgeCount=0;

			//Calculate the force each node is feeling
			for(int i=0;i<nodes.Count;i++){
				n=nodes[i];
				//Make sure this node is active (not part of a mega node)
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
				//Get force from edges
				Vector3 edgeF;
				for(int j=0;j<n.edges.Count;j++){
					//Could also apply edge force to both here - future improvement
					edgeF = n.edges[j].GetForce(n);
					n.force+=edgeF;

					count++;

					if(count>frameLimit){
						count=0;
						yield return null;
					}
				}
				n.ApplyForce(dt);
			}

			//Process Mega Nodes Edges and mega-mega repulsion
			for(int i=0; i<megas.Count; i++)
			{
				n=megas[i];
				//Make sure this mega is not decompressed
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
					//Could also apply edge force to both here - future improvement
					edgeF = n.edges[j].GetForce(n);
					n.force+=edgeF;

					count++;

					if(count>frameLimit){
						count=0;
						yield return null;
					}
				}
				n.ApplyForce(dt);
			}

			//Actually apply the forces
			//If we're checking the positions to see wether to slow/end the simulation, do so
			Vector3 dif = Vector3.zero;
			int difCount=0;
			//Process regular nodes
			for(int i=0;i<nodes.Count;i++){
				Vector2 edgeInfo = nodes[i].UpdatePos();
				relevantEdgeCount+=(int)edgeInfo.x;
				totalDist+=edgeInfo.y;

				if(framesUntilCheck<0 && nodes[i].simulating)
				{
					dif+=nodes[i].difFromHistory();
					nodes[i].recordHistory();
					difCount++;
				}
			}
			//Process mega nodes
			for(int i=0;i<megas.Count;i++){
				Vector2 edgeInfo = megas[i].UpdatePos();
				relevantEdgeCount+=(int)edgeInfo.x;
				totalDist+=edgeInfo.y;

				if(framesUntilCheck<0 && megas[i].simulating)
				{
					dif+=nodes[i].difFromHistory();
					nodes[i].recordHistory();
					difCount++;
				}
			}

			Edge.avLen = totalDist/relevantEdgeCount;

			//Results of history check
			if(framesUntilCheck<0)
			{
				//Check the average change in position from a while back
				//If its below a threshhold, slow down, and if simplified, un-simplify
				//Reset timeuntil and history check
				dif/=difCount+0f;
				//Debug.Log(dif.magnitude);
				if(dif.magnitude<.05f*dt)
				{
					if(dt<.01f)
					{
						upNodes=false;
						simulating = false;
						framesUntilCheck = 50;
						saveSolvedFile();
					}else
					{

						if(simplify)
						{
							simplify = false;
							StartCoroutine(Unsimplify());
							framesUntilCheck = 150;
							framesPerCheck -= 10;
						}else
						{
							dt*=.5f;
							framesUntilCheck = 50;
							framesPerCheck -= 10;
						}
					}
				}else
				{
					framesUntilCheck = framesPerCheck;
				}
			}

			yield return null;
		}
	}

	/**
		Saves a file that denotes the "solved" positions of the nodes
		Name of the file will be the same as the unsolved, but with fileType .mtxs

		Line 1 will have the number of nodes and edges
		Next will be all the nodes: index ID position.x position.y position.z
		Then the Edges: index1 index2

	*/
	void saveSolvedFile(){
		string path = Application.dataPath+"/SolvedMatrices/"+file+".mtxs";
		//If it's already been saved, don't bother saving it again
		if(File.Exists(path))
		{
			return;
		}

		//Put together the file
		string fileData = nodes.Count+" "+edges.Count;

		for(int i=0;i<nodes.Count;i++)
		{
			if(!nodes[i].simulating) continue;

			Vector3 pos = nodes[i].t.localPosition;
			fileData += "\n"+i+" "+nodes[i].id+" "+
				pos.x.ToString("#.000")+" "+pos.y.ToString("#.000")+" "+pos.z.ToString("#.000");
		}

		for(int i=0;i<edges.Count;i++)
		{
			if(edges[i].t.localPosition==Vector3.right*99999) continue;

			fileData += "\n"+edges[i].node1.index+" "+edges[i].node2.index;
		}

		File.WriteAllText(path, fileData);
	}

	/**
		Unsimplifies graph
		just calls StopSim() on  active mega nodes over time
	*/
	IEnumerator Unsimplify(){
		for(int i=0;i<megas.Count;i++)
		{
			megas[i].StopSim();
			if(i%5==0) yield return new WaitForSeconds(.1f);
		}
		yield break;
	}

	/**
		Creates an Edge object, connects it to nodes, and pushes it to edges
		@param n1 First node this edge should connect to. Can be a Mega.
		@param n2 Second node this edge should connect to. Can be a Mega.
	*/
	void CreateEdge(Node n1, Node n2){
		//Make sure it's not an edge to itself
		if(n1==n2) return;

		Edge e = (Instantiate(edgeFab) as Transform).GetComponent<Edge>();
		e.n1=n1;
		e.n2=n2;
		n1.AddEdge(e);
		n2.AddEdge(e);
		e.name="edge"+n1.id+"-"+n2.id;
		e.t.SetParent(container);
		edges.Add(e);
	}

	/**
		Creates a Node object, initializes some properites, and pushes it to nodes
		@param id the row/column number from the matrix file
		@param pos Position determined from location in the matrix
	*/
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

	/**
		Creates a mega node. It gets pushed into a list elsewhere.
		Initializes some properties
		@param id The index it will inhabit in the megas list
	*/
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
