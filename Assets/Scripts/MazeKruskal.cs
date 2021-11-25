using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クラスカル法で迷路を計算を行うクラス
/// </summary>
public static class MazeKruskal
{
	/// <summary>
	/// 迷路の状態の計算を行う
	/// </summary>
	/// <param name="edges">エッジ</param>
	/// <param name="nodeCount">ノード数</param>
	public static void Calculate(List<Edge> remainedEdges, Node[] nodes)
	{

		for (var i = 0; i < nodes.Length; i++)
		{
			nodes[i].group = i;
		}

		// 未処理のエッジが無くなるまでループ
		while (remainedEdges.Count != 0)
		{
			// エッジをランダムに選択
			int randomIndex = Random.Range(0, remainedEdges.Count);
			Edge currentEdge = remainedEdges[randomIndex];
			remainedEdges.RemoveAt(randomIndex);

			// エッジのノードが同じグループに属しているかチェック
			var n0 = nodes[currentEdge.node0];
			var n1 = nodes[currentEdge.node1];
			if (n0.group == n1.group)
			{
				currentEdge.isConnected = false;
				continue;
			}

			// ノードが別のグループに属している場合、
			// エッジを接続状態にし、ノードを同じグループに移動
			currentEdge.isConnected = true;
			if (n0.group < n1.group) n1.ChangeGroup(n0.group);
			else n0.ChangeGroup(n1.group);
		}
	}
}

/// <summary>
/// ノード
/// </summary>
public class Node
{
	/// <summary>
	/// グループ
	/// </summary>
	public int group;

	/// <summary>
	/// 隣接ノード
	/// </summary>
	public List<Node> nodes = new List<Node>();

	// グループ変更キュー
	private Queue<Node> targets = new Queue<Node>();

	/// <summary>
	/// 新規ノードを追加する
	/// </summary>
	/// <param name="newNode">新規ノード</param>
	public void AddNode(Node newNode)
	{
		if (nodes.Contains(newNode)) return;
		nodes.Add(newNode);
	}

	/// <summary>
	/// グループを変更する
	/// </summary>
	/// <param name="newGroup">変更後のグループ</param>
	public void ChangeGroup(int newGroup)
	{
		targets.Enqueue(this);
		while(targets.Count > 0)
		{
			var targetNode = targets.Dequeue();
			foreach(var node in targetNode.nodes)
			{
				if (node.group == newGroup) continue;
				if (node.group == targetNode.group) targets.Enqueue(node);
			}
			targetNode.group = newGroup;
		}
	}
}

/// <summary>
/// エッジ
/// </summary>
public class Edge
{
	/// <summary>
	/// ノード0のindex
	/// </summary>
	public int node0;

	/// <summary>
	/// ノード1のindex
	/// </summary>
	public int node1;

	/// <summary>
	/// 接続状態
	/// </summary>
	public bool isConnected;
}