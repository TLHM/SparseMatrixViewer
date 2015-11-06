# SparseMatrixViewer
Unity3D project to view sparse matrices as graphs in 3D space.
Built and tested using Unity 5.1.1f1

This project is meant to be run in the Unity Editor, though a compiled version may be created in the future.

Currently, the only supported file type is .mtx, and matrices are assumed to be symmetrical and without duplicate edges.

# Usage

To view a matrix, change the "file" property of the Controller object in the Unity Editor inspector to be the name of any .mtx file in the Assets/Martices folder of the project.

Hit the play button in the unity editor, and the matrix will be loaded and then undergo the simulation process to determine an stable shape, which may take some time.

During simulation, the mouse can be used to control the viewing angle (click and drag) and the zoom of the camera (scroll).

Hitting SpaceBar will stop and resume the simulation.

# Methodology

Graphs structure is determined by two simple rules:

Nodes repel each other.

Edges draw both nodes towards each other.

The simplification algorithm is also fairly straight-forward. The basic idea is that mega nodes are created by grouping nodes with similar or identical neighbors (nodes that they are connected to via edges).

# Details

For detailed information on function, check the script files.

Documentation is found in Docs/ but only covers public members and methods.

# Future work

- Output finished graph structures that can be viewed again at a later time.
- Add Graphical indicators of loading and simplifying process.
