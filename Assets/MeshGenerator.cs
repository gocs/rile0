using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour {

	public SquareGrid squareGrid;
	public MeshFilter walls;
	public MeshFilter cave;

	public bool is2d;

	List<Vector3> vertices;
	List<int> triangles;
	
	Dictionary<int, List<Triangle>> triangleDict = new Dictionary<int, List<Triangle>>();
	List<List<int>> outlinesList = new List<List<int>>();
	HashSet<int> checkedVertices = new HashSet<int>();

	public void GenerateMesh(int [,] map, float squareSize) {

		triangleDict.Clear();
		outlinesList.Clear();
		checkedVertices.Clear();

		squareGrid = new SquareGrid(map, squareSize);

		vertices = new List<Vector3>();
		triangles = new List<int>();

		for (int x = 0; x < squareGrid.squares.GetLength(0); x++) {
			for (int y = 0; y < squareGrid.squares.GetLength(1); y++) {
				TriangulateSquare(squareGrid.squares[x,y]);
			}
		}

		Mesh mesh = new Mesh();
		cave.mesh = mesh;

		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();

		Vector2[] uvs = new Vector2[vertices.Count];
		
		for (int i = 0; i < vertices.Count; i ++) {
			float percentX = Mathf.InverseLerp(-map.GetLength(0)*squareSize, map.GetLength(1)*squareSize, vertices[i].x);
			float percentY = Mathf.InverseLerp(-map.GetLength(0)*squareSize, map.GetLength(1)*squareSize, vertices[i].z);
			uvs[i] = new Vector2(percentX, percentY);
		}

		mesh.uv = uvs;

		if (is2d) {
			Create2dWallMesh();
		} else {
			CreateWallMesh();
		}
	}

	void CreateWallMesh() {

		CalculateMeshOutLines();

		List<Vector3> wallVertices = new List<Vector3>();
		List<int> wallTriangles = new List<int>();
		Mesh wallMesh = new Mesh();
		float wallheight = 5;

		foreach (List<int> outline in outlinesList) {
			for (int i = 0; i < outline.Count -1; i++) {
				int start = wallVertices.Count;
				wallVertices.Add(vertices[outline[i]]);
				wallVertices.Add(vertices[outline[i +1]]);
				wallVertices.Add(vertices[outline[i]] -Vector3.up *wallheight);
				wallVertices.Add(vertices[outline[i +1]] -Vector3.up *wallheight);

				wallTriangles.Add(start +0);
				wallTriangles.Add(start +2);
				wallTriangles.Add(start +3);

				wallTriangles.Add(start +3);
				wallTriangles.Add(start +1);
				wallTriangles.Add(start +0);
			}
		}
		wallMesh.vertices = wallVertices.ToArray();
		wallMesh.triangles = wallTriangles.ToArray();

		walls.mesh = wallMesh;

		MeshCollider wallCollider = walls.gameObject.AddComponent<MeshCollider> ();
		wallCollider.sharedMesh = wallMesh;
	}

	void Create2dWallMesh() {
		EdgeCollider2D[] currentColliders = gameObject.GetComponents<EdgeCollider2D>();

		for (int i = 0; i < currentColliders.Length; i ++) {
			Destroy(currentColliders[i]);
		}

		CalculateMeshOutLines();

		foreach (List<int> outline in outlinesList) {
			EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
			Vector2[] edgePts = new Vector2[outline.Count];

			for (int i = 0; i < outline.Count; i ++) {
				edgePts[i] = new Vector2(vertices[outline[i]].x, vertices[outline[i]].z);
			}
			edgeCollider.points = edgePts;
		}
	}

	void TriangulateSquare(Square square) {
		switch (square.config) {
			case 0: break;

	        // 1 points:
	        case 1: MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft); break;
	        case 2: MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight); break;
	        case 4: MeshFromPoints(square.topRight, square.centerRight, square.centerTop); break;
	        case 8: MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft); break;

	        // 2 points:
	        case 3: MeshFromPoints(square.centerRight, square.bottomRight, square.bottomLeft, square.centerLeft); break;
	        case 6: MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom); break;
	        case 9: MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft); break;
	        case 12: MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft); break;

			// 2 points
	        case 5: MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft); break;
	        case 10: MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft); break;

	        // 3 point:
	        case 7: MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft); break;
	        case 11: MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft); break;
	        case 13: MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft); break;
	        case 14: MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft); break;

	        // 4 point:
	        case 15:
	            MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
	            checkedVertices.Add(square.topLeft.vertexIndex);
	            checkedVertices.Add(square.topRight.vertexIndex);
	            checkedVertices.Add(square.bottomRight.vertexIndex);
	            checkedVertices.Add(square.bottomLeft.vertexIndex);
	            break;
		}
	}

	void MeshFromPoints(params Node[] points) {
		AssignVerteces(points);

		if (points.Length >= 3)
			CreateTriangle(points[0], points[1], points[2]);
		if (points.Length >= 4)
			CreateTriangle(points[0], points[2], points[3]);
		if (points.Length >= 5)
			CreateTriangle(points[0], points[3], points[4]);
		if (points.Length >= 6)
			CreateTriangle(points[0], points[4], points[5]);
	}

	void AssignVerteces(Node[] points) {
		for (int i = 0; i < points.Length; i++) {
			if (points[i].vertexIndex == -1) {
				points[i].vertexIndex = vertices.Count;
				vertices.Add(points[i].position);
			}
		}
	}

	void CreateTriangle(Node a, Node b, Node c) {
		triangles.Add(a.vertexIndex);
		triangles.Add(b.vertexIndex);
		triangles.Add(c.vertexIndex);

		Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
		AddTriangleToDict(triangle.vertexIndexA, triangle);
		AddTriangleToDict(triangle.vertexIndexB, triangle);
		AddTriangleToDict(triangle.vertexIndexC, triangle);
	}

	void AddTriangleToDict(int vertexIndex, Triangle triangle) {
		if (triangleDict.ContainsKey(vertexIndex)) {
			triangleDict[vertexIndex].Add(triangle);
		} else {
			List<Triangle> triangleList = new List<Triangle>();
			triangleList.Add(triangle);
			triangleDict.Add(vertexIndex, triangleList);
		}
	}

	void CalculateMeshOutLines() {
		for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++) {
			if (!checkedVertices.Contains(vertexIndex)) {
				int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);

				if (newOutlineVertex != -1) {
					checkedVertices.Add(vertexIndex);

					List<int> newOutline = new List<int>();
					newOutline.Add(vertexIndex);
					outlinesList.Add(newOutline);
					FollowOutline(newOutlineVertex, outlinesList.Count -1);
					outlinesList[outlinesList.Count -1].Add(vertexIndex);
				}
			}
		}
	}

	void FollowOutline(int newOutlineVertex, int outlinesListCount) {
		outlinesList[outlinesListCount].Add(newOutlineVertex);
		checkedVertices.Add(newOutlineVertex);
		int nextVertexIndex = GetConnectedOutlineVertex(newOutlineVertex);

		if (nextVertexIndex != -1)
			FollowOutline(nextVertexIndex, outlinesListCount);
	}

	int GetConnectedOutlineVertex(int vertexIndex) {
		List<Triangle> triangleListWithVertex = triangleDict[vertexIndex];

		for (int i = 0; i < triangleListWithVertex.Count; i++) {
			Triangle triangle = triangleListWithVertex [i];

			for (int j = 0; j < 3; j ++) {
				int vertexB = triangle [j];

				if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB)) {
					if (IsOutlineEdge(vertexIndex, vertexB)) {
						return vertexB;
					}
				}
			}
		}

		return -1;
	}

	bool IsOutlineEdge(int vertexA, int vertexB) {
		List<Triangle> triangleDictWithVertexA = triangleDict[vertexA];
		int sharedIntCount = 0;

		for (int i = 0; i < triangleDictWithVertexA.Count; i++) {
			if (triangleDictWithVertexA[i].Contains(vertexB)) {
				sharedIntCount ++;
				if (sharedIntCount > 1) {
					break;
				}
			}
		}

		return sharedIntCount == 1;
	}

	struct Triangle {
		public int vertexIndexA;
		public int vertexIndexB;
		public int vertexIndexC;

		int[] vertices;

		public Triangle(int a, int b, int c) {
			vertexIndexA = a;
			vertexIndexB = b;
			vertexIndexC = c;

			vertices = new int[3];
			vertices[0] = a;
			vertices[1] = b;
			vertices[2] = c;
		}

		public int this[int i] {
			get { return vertices[i]; }
		}

		public bool Contains(int vertexIndex) {
			return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
		}
	}

	public class SquareGrid {
		public Square[,] squares;
		public SquareGrid(int[,] map, float squareSize) {
			int nodeCountX = map.GetLength(0);
			int nodeCountY = map.GetLength(1);

			float mapWidth = nodeCountX * squareSize;
			float mapHeight = nodeCountY * squareSize;

			ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

			for (int x = 0; x < nodeCountX; x++) {
				for (int y = 0; y < nodeCountY; y++) {
					Vector3 pos = new Vector3(
						-mapWidth/2 + x + squareSize + squareSize/2,
						0,
						-mapHeight/2 + y + squareSize + squareSize/2
					);
					controlNodes[x,y] = new ControlNode(pos, map[x,y] == 1, squareSize);
				}
			}

			squares = new Square[nodeCountX -1, nodeCountY -1];
			for (int x = 0; x < nodeCountX -1; x++) {
				for (int y = 0; y < nodeCountY -1; y++) {
					squares[x,y] = new Square(
						controlNodes[x, y +1],
						controlNodes[x +1, y +1],
						controlNodes[x +1, y],
						controlNodes[x, y]
					);
				}
			}
		}
	}

	public class Square {
		public ControlNode topLeft, topRight, bottomRight, bottomLeft;
		public Node centerTop, centerRight, centerBottom, centerLeft;

		public int config = 0;

		public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft) {
			topLeft = _topLeft;
			topRight = _topRight;
			bottomLeft = _bottomLeft;
			bottomRight = _bottomRight;

			centerTop = topLeft.right;
			centerRight = bottomRight.above;
			centerBottom = bottomLeft.right;
			centerLeft = bottomLeft.above;

			if (topLeft.active) config += 8;
			if (topRight.active) config += 4;
			if (bottomRight.active) config += 2;
			if (bottomLeft.active) config += 1;
		}
	}

	public class Node {
		public Vector3 position;
		public int vertexIndex = -1;

		public Node(Vector3 _pos) {
			position = _pos;
		}
	}

	public class ControlNode : Node {
		public bool active;

		public Node above, right;

		public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos) {
			active = _active;
			above = new Node(position + Vector3.forward * squareSize/2f);
			right = new Node(position + Vector3.right * squareSize/2f);
		}
	}
}
