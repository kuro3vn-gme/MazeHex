using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hex迷路を生成するスクリプト。
/// </summary>
public class MazeHex : MonoBehaviour
{
	/// <summary>
	/// 壁の幅
	/// </summary>
	public float wallWidth = 0.1f;

	/// <summary>
	/// 壁の高さ
	/// </summary>
	public float wallHeight = 0.5f;

	/// <summary>
	/// 六角形の内接円の半径
	/// </summary>
	public float hexRadius = 1f;

	/// <summary>
	/// 迷路の大きさ
	/// </summary>
	public int mapSize = 3;

	/// <summary>
	/// 壁のマテリアル</summary>
	[SerializeField]
	private Material wallMat;

	/// <summary>
	/// 三角柱のマテリアル
	/// </summary>
	[SerializeField]
	private Material prismMat;

	/// <summary>
	/// 壁生成時の親オブジェクト
	/// </summary>
	[SerializeField]
	private Transform wallParent;

	/// <summary>
	/// 三角柱生成時の親オブジェクト
	/// </summary>
	[SerializeField]
	private Transform prismParent;

	// 壁リスト
	private List<Wall> walls = new List<Wall>();

	// 三角柱リスト
	private List<GameObject> prisms = new List<GameObject>();

	// 六角形二次元配列
	private Hexagon[,] hexArray;
	private int hexArrayWidth;
	private int hexArrayHeight;
	private int hexCount;

	private List<Edge> edges;
	private List<Edge> remainedEdges;
	private Node[] nodes;

	// 六角形生成キュー
	private Queue<(Vector3Int position, int range)> creationQueue = new Queue<(Vector3Int, int)>();

	// 三角柱メッシュ
	private Mesh triangularPrismMesh = null;

	// 壁キャッシュ
	private GameObject wallCash = null;

	// 三角形キャッシュ
	private GameObject prismCash = null;

	// 六角形の外接円半径
	private float circumRadius;

	// Cube座標のX方向ユニット
	private Vector3 xUnitVector;

	// Cube座標のY方向ユニット
	private Vector3 yUnitVector;

	// Cube座標のZ方向ユニット
	private Vector3 zUnitVector;

	// Cube座標における隣接座標
	private Vector3Int[] cubeDirectionVectors = new[]
{
		new Vector3Int(0, 1, -1),
		new Vector3Int(1, 0, -1),
		new Vector3Int(1, -1, 0),
		new Vector3Int(0, -1, 1),
		new Vector3Int(-1, 0, 1),
		new Vector3Int(-1, 1, 0)
	};

	private static readonly float root3 = Mathf.Sqrt(3);
	private static readonly float invRoot3 = 1 / root3;

	public bool IsInitialized { get; private set; }

	private void Start()
	{
		CreateHexMap();
		CreateMaze();
	}

	/// <summary>
	/// 六角形を二次元配列から取得する
	/// </summary>
	/// <param name="cubePosition">Cube座標</param>
	/// <returns>六角形</returns>
	private Hexagon GetHex(Vector3Int cubePosition)
	{
		var x = cubePosition.x + mapSize + 1;
		var y = cubePosition.y + mapSize + 1;

		return hexArray[x, y];
	}

	/// <summary>
	/// 六角形を二次元配列に格納する
	/// </summary>
	/// <param name="cubePosition">Cube座標</param>
	/// <param name="hex">六角形</param>
	private void SetHex(Vector3Int cubePosition, Hexagon hex)
	{
		var x = cubePosition.x + mapSize + 1;
		var y = cubePosition.y + mapSize + 1;
		hexArray[x, y] = hex;
		hexCount++;
	}

	/// <summary>
	/// Cube座標からUnity上の座標に変換する
	/// </summary>
	/// <param name="cubePosition">Cube座標</param>
	/// <returns>Unity上の座標</returns>
	public Vector3 GetPosition(Vector3Int cubePosition)
	{
		return xUnitVector * cubePosition.x + yUnitVector * cubePosition.y + zUnitVector * cubePosition.z;
	}

	/// <summary>
	/// 0～5のIndexをCube座標における隣接座標に変換する
	/// </summary>
	/// <param name="index">インデックス</param>
	/// <returns>Cube座標における隣接座標</returns>
	public Vector3Int GetCubeDirection(int index)
	{
		return cubeDirectionVectors[index % 6];
	}

	/// <summary>
	/// 0～5のIndexから六角形上の反対方向のIndexを取得する
	/// </summary>
	/// <param name="index">インデックス</param>
	/// <returns>反対方向のインデックス</returns>
	public int GetOppositeDirectionIndex(int index)
	{
		switch (index)
		{
			case 0: return 3;
			case 1: return 4;
			case 2: return 5;
			case 3: return 0;
			case 4: return 1;
			case 5: return 2;
			default: return GetOppositeDirectionIndex(index % 6);
		}
	}

	/// <summary>
	/// マップの状態を初期化する
	/// </summary>
	private void Initialize()
	{
		// マップの消去
		ClearMap();
		var cap = (int)((1 + mapSize) * mapSize * 0.5f) * 6;
		wallParent.hierarchyCapacity = cap;
		prismParent.hierarchyCapacity = cap;

		// 外接円を計算
		circumRadius = hexRadius * 2 * invRoot3;

		// X, Y, Zユニットのサイズを計算
		xUnitVector = new Vector3(circumRadius, 0, 0);
		yUnitVector = new Vector3(-circumRadius * 0.5f, 0f, hexRadius);
		zUnitVector = new Vector3(-circumRadius * 0.5f, 0f, -hexRadius);

		hexArrayWidth = (mapSize + 1) * 2 + 1;
		hexArrayHeight = (mapSize + 1) * 2 + 1;
		hexArray = new Hexagon[hexArrayWidth, hexArrayHeight];
		hexCount = 0;

		IsInitialized = true;
	}

	/// <summary>
	/// マップを消去する
	/// </summary>
	private void ClearMap()
	{
		// 壁消去
		foreach (var wall in walls)
		{
			wall.go.SetActive(false);
			DestroyImmediate(wall.go);
		}
		walls.Clear();

		// 三角柱消去
		foreach (var prism in prisms)
		{
			prism.SetActive(false);
			DestroyImmediate(prism);
		}
		prisms.Clear();

		// 生成キュー消去
		creationQueue.Clear();

		// キャッシュ消去
		DestroyImmediate(wallCash);
		DestroyImmediate(prismCash);
		wallCash = null;
		prismCash = null;

		// 三角柱メッシュ消去
		if (triangularPrismMesh != null)
		{
			triangularPrismMesh.Clear();
		}
	}

	/// <summary>
	/// 六角マップを作成する
	/// </summary>
	public void CreateHexMap()
	{
		// 初期化
		Initialize();

		// 六角形を順次生成する
		creationQueue.Enqueue(new (Vector3Int.zero, mapSize));
		while (creationQueue.Count != 0)
		{
			var target = creationQueue.Dequeue();
			if (GetHex(target.position) != null) continue;

			CreateHexagon(target.position, target.range);
		}

		// 壁情報を元にエッジのリストを生成
		edges = new List<Edge>();
		foreach (var wall in walls)
		{
			if (wall.hex1 == -1 || wall.hex2 == -1) continue;
			var edge = new Edge();
			edge.node0 = wall.hex1;
			edge.node1 = wall.hex2;
			edges.Add(edge);
		}
		remainedEdges = new List<Edge>();
		remainedEdges.Capacity = edges.Capacity;

		// ノードを生成する
		nodes = new Node[hexCount];
		for (var i = 0; i < hexCount; i++)
		{
			var node = new Node();
			node.group = i;
			nodes[i] = node;
		}

		// 隣接ノードへの参照を作成
		for (var i = 0; i < edges.Count; i++)
		{
			nodes[edges[i].node0].AddNode(nodes[edges[i].node1]);
			nodes[edges[i].node1].AddNode(nodes[edges[i].node0]);
		}
	}

	/// <summary>
	/// 迷路を生成する
	/// </summary>
	public void CreateMaze()
	{
		remainedEdges.AddRange(edges);

		// エッジの接続状態を計算
		MazeKruskal.Calculate(remainedEdges, nodes);

		// 壁のON/OFFを切り替える
		var edgeIndex = 0;
		foreach(var wall in walls)
		{
			if (wall.hex1 == -1 || wall.hex2 == -1)
			{
				wall.Show();
				continue;
			}
			if (edges[edgeIndex].isConnected)
			{
				wall.Hide();
			}
			else
			{
				wall.Show();
			}
			edgeIndex++;
		}
	}

	/// <summary>
	/// 六角形の生成する
	/// </summary>
	/// <param name="cubePosition">Cube座標</param>
	/// <param name="creationCount">カウントが0になるまで隣接する六角形を生成</param>
	private void CreateHexagon(Vector3Int cubePosition, int creationCount)
	{
		// 六角形を初期化
		var hexagon = new Hexagon();
		hexagon.cubePosition = cubePosition;
		hexagon.index = hexCount;

		// Cube座標からUnity座標に変換
		var centerPosition = GetPosition(cubePosition);

		// 壁を生成する
		for(var i = 0; i < 6; i++)
		{
			Wall wall;

			// 既に壁の方向に六角形が存在する場合は壁を生成しない
			var hex = GetHex(cubePosition + GetCubeDirection(i));
			if (hex != null)
			{
				// wallに六角形のIndexを登録しておく
				wall  = hex.walls[GetOppositeDirectionIndex(i)];
				hexagon.walls[i] = wall;
				wall.hex2 = hexagon.index;
				continue;
			}

			// 壁オブジェクト生成
			var wallGO = CreateWall(wallParent);

			// 位置、回転、サイズ設定
			wallGO.transform.localScale = new Vector3(wallWidth, wallHeight, circumRadius - wallWidth * invRoot3);
			var wallPosition = GetPosition(GetCubeDirection(i)) * 0.5f + new Vector3(0, wallHeight * 0.5f, 0);
			wallGO.transform.localPosition = centerPosition + wallPosition;
			wallGO.transform.localRotation = Quaternion.Euler(0, 90 + 60 * i, 0);

			// リスト登録
			wall = new Wall(wallGO);
			walls.Add(wall);

			// 六角形と紐づけ
			hexagon.walls[i] = wall;
			wall.hex1 = hexagon.index;
		}

		// 三角柱の生成
		for(var i = 0; i < 6; i++)
		{
			// 既に三角柱が存在する場合は処理をやめる
			if (GetHex(cubePosition + GetCubeDirection(i)) != null) continue;
			if (GetHex(cubePosition + GetCubeDirection(i + 1)) != null) continue;

			// 三角柱オブジェクト生成
			var prism = CreateTriangularPrism(prismParent);

			// 位置、回転、サイズ設定
			var prismPosition = new Vector3(
				circumRadius * Mathf.Sin((30 + i * 60) * Mathf.Deg2Rad),
				wallHeight * 0.5f,
				circumRadius * Mathf.Cos((30 + i * 60) * Mathf.Deg2Rad));
			prism.transform.localPosition = centerPosition + prismPosition;
			prism.transform.localRotation = Quaternion.Euler(0, 90 + 60 * i, 0);

			prism.SetActive(true);
			prisms.Add(prism);
		}

		SetHex(cubePosition, hexagon);

		// creationCountが0以上の場合は、隣接する六角形を生成する
		if (creationCount > 0)
		{
			for(var i = 0; i < 6; i++)
			{
				creationQueue.Enqueue(new (cubePosition + GetCubeDirection(i), creationCount - 1));
			}
		}
	}

	/// <summary>
	/// 壁を生成する
	/// </summary>
	/// <returns>壁のGameObject</returns>
	private GameObject CreateWall(Transform parent)
	{
		// キャッシュが存在する場合、キャッシュを返す
		if (wallCash != null) return Instantiate(wallCash, parent);

		// キャッシュ生成
		var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
		go.name = "Wall";
		var meshRenderer = go.GetComponent<MeshRenderer>();
		meshRenderer.material = wallMat;
		wallCash = go;
		wallCash.SetActive(false);

		return Instantiate(wallCash, parent);
	}

	/// <summary>
	/// 三角柱のGameObjectを生成する
	/// </summary>
	/// <returns>三角柱のGameObject</returns>
	private GameObject CreateTriangularPrism(Transform parent)
	{
		// キャッシュが存在する場合、キャッシュを返す
		if (prismCash != null) return Instantiate(prismCash, parent);

		// 三角柱メッシュを生成
		triangularPrismMesh = CreateTriangularPrismMesh();

		// キャッシュ生成
		var go = new GameObject();
		go.name = "TriangularPrism";
		var meshFilter = go.AddComponent<MeshFilter>();
		meshFilter.mesh = triangularPrismMesh;
		var meshRenderer = go.AddComponent<MeshRenderer>();
		meshRenderer.material = prismMat;
		prismCash = go;
		prismCash.SetActive(false);

		return Instantiate(prismCash, parent);
	}

	/// <summary>
	/// 三角柱のメッシュを生成する
	/// </summary>
	/// <returns>三角柱のメッシュ</returns>
	private Mesh CreateTriangularPrismMesh()
	{
		var mesh = new Mesh();

		// 座標の計算
		var z = wallWidth * invRoot3;
		var x = wallWidth * 0.5f;
		var y = wallHeight * 0.5f;
		var v0 = new Vector3(0, -y, z);
		var v1 = new Vector3(x, -y, -z * 0.5f);
		var v2 = new Vector3(-x, -y, -z * 0.5f);
		var v3 = new Vector3(0, y, z);
		var v4 = new Vector3(x, y, -z * 0.5f);
		var v5 = new Vector3(-x, y, -z * 0.5f);

		var vertices = new Vector3[]
		{
			v0, v1, v2, // 底面(0, 1, 2)
			v0, v1, v3, v4, // 側面1(3, 4, 5, 6)
			v1, v2, v4, v5, // 側面2(7, 8, 9, 10)
			v2, v0, v5, v3, // 側面3(11, 12, 13, 14)
			v3, v4, v5 // 上面(15, 16, 17)
		};

		var triangles = new int[]
		{
			0, 2, 1,	// 底面
			3, 4, 5,	// 側面1
			4, 6, 5,	// 側面1
			7, 8, 9,	// 側面2
			8, 10, 9,	// 側面2
			11, 12, 13,	// 側面3
			12, 14, 13,	// 側面3
			15, 16, 17	// 上面
		};

		mesh.SetVertices(vertices);
		mesh.SetTriangles(triangles, 0);

		// BoundsとNormalを計算
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();

		return mesh;
	}

	/// <summary>
	/// 六角形
	/// </summary>
	public class Hexagon
	{
		public int index;
		public Vector3Int cubePosition;
		public Wall[] walls = new Wall[6];
	}

	/// <summary>
	/// 壁
	/// </summary>
	public class Wall
	{
		public int hex1 = -1;
		public int hex2 = -1;
		public GameObject go;
		private Vector3 position;
		private Vector3 hidePosition;

		public Wall(GameObject go)
		{
			this.go = go;
			position = go.transform.localPosition;
			hidePosition = new Vector3(position.x, -position.y -0.1f, position.z);
			go.SetActive(true);
		}

		public void Show()
		{
			go.transform.localPosition = position;
		}

		public void Hide()
		{
			go.transform.localPosition = hidePosition;
		}
	}
}