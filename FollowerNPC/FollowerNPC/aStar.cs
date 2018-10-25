using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using xTile.Dimensions;

namespace FollowerNPC
{
    public class aStar
    {
        public aStar(GameLocation location)
        {
            gameLocation = location;
        }

        public GameLocation gameLocation
        {
            get { return gameLocation; }
            set
            {
                gameLocation = value;
                dimensions = new Vector2(gameLocation.map.Layers[0].LayerWidth, gameLocation.map.Layers[0].LayerHeight);
            }
        }
        private Vector2 dimensions;
        private Vector2 negativeOne = new Vector2(-1, -1);

        public Queue<Vector2> Pathfind(Vector2 start, Vector2 goal)
        {
            PriorityQueue open = new PriorityQueue(new Node(null, start));
            Dictionary<Vector2, float> closed = new Dictionary<Vector2, float>();
            Vector2 mapDimensions = new Vector2(100, 100);
            List<Node> path = new List<Node>();

            while (open.Count != 0)
            {
                Node q = open.Dequeue();
                Node[] successors = GetSuccessors(q);
                foreach (Node successor in successors)
                {
                    if (!(successor != null))
                        continue;

                    if (successor.position == goal)
                    {
                        path = Reconstruct(successor);
                        goto NodeConsolidation;
                    }

                    successor.g = q.g + EuclideanDistanceSquared(q.position, successor.position);
                    bool locationOpen = gameLocation.isTileLocationOpen(new Location((int)successor.position.X, (int)successor.position.Y));
                    //ModEntry.monitor.Log("null check & goal check & g set & locationOpen set");
                    successor.h = EuclideanDistanceSquared(successor.position, goal) + (locationOpen ? 0f : mapDimensions.LengthSquared());
                    successor.f = successor.g + successor.h;

                    if (open.CheckValue(successor.position) < successor.f)
                        break;

                    if (closed.TryGetValue(successor.position, out float closedNodeCost) && closedNodeCost < successor.f)
                        break;

                    open.Enqueue(successor);
                }
            }
            return null;
            NodeConsolidation:
            Queue<Vector2> consolidatedPath = new Queue<Vector2>();
            foreach (Node node in path)
            {
                if (IsCorner(node.position))
                    consolidatedPath.Enqueue(node.position);
            }
            return consolidatedPath;
        }

        private Vector2[] GetNeighbors(Vector2 tile)
        {
            float ppx = tile.X;
            float ppy = tile.Y;
            float mdx = dimensions.X;
            float mdy = dimensions.Y;
            Vector2[] successors = new Vector2[8];
            successors[0] = ppy > 0 ? new Vector2(ppx, ppy - 1) : negativeOne;
            successors[1] = ppy > 0 && ppx < mdx - 1 ? new Vector2(ppx + 1, ppy - 1) : negativeOne;
            successors[2] = ppx < mdx - 1 ? new Vector2(ppx + 1, ppy) : negativeOne;
            successors[3] = ppy < mdy - 1 && ppx < mdx - 1 ? new Vector2(ppx + 1, ppy + 1) : negativeOne;
            successors[4] = ppy < mdy - 1 ? new Vector2(ppx, ppy + 1) : negativeOne;
            successors[5] = ppy < mdy - 1 && ppx > 0 ? new Vector2(ppx - 1, ppy + 1) : negativeOne;
            successors[6] = ppx > 0 ? new Vector2(ppx - 1, ppy) : negativeOne;
            successors[7] = ppy > 0 && ppx > 0 ? new Vector2(ppx - 1, ppy - 1) : negativeOne;
            return successors;
        }

        private Node[] GetSuccessors(Node parent)
        {
            float ppx = parent.position.X;
            float ppy = parent.position.Y;
            float mdx = dimensions.X;
            float mdy = dimensions.Y;
            Node[] successors = new Node[8];
            successors[0] = ppy > 0 ? new Node(parent, new Vector2(ppx, ppy - 1)) : null;
            successors[1] = ppy > 0 && ppx < mdx - 1 ? new Node(parent, new Vector2(ppx + 1, ppy - 1)) : null;
            successors[2] = ppx < mdx - 1 ? new Node(parent, new Vector2(ppx + 1, ppy)) : null;
            successors[3] = ppy < mdy - 1 && ppx < mdx - 1 ? new Node(parent, new Vector2(ppx + 1, ppy + 1)) : null;
            successors[4] = ppy < mdy - 1 ? new Node(parent, new Vector2(ppx, ppy + 1)) : null;
            successors[5] = ppy < mdy - 1 && ppx > 0 ? new Node(parent, new Vector2(ppx - 1, ppy + 1)) : null;
            successors[6] = ppx > 0 ? new Node(parent, new Vector2(ppx - 1, ppy)) : null;
            successors[7] = ppy > 0 && ppx > 0 ? new Node(parent, new Vector2(ppx - 1, ppy - 1)) : null;
            return successors;
        }

        private List<Node> Reconstruct(Node goal)
        {
            Node iterator = goal;
            List<Node> ret = new List<Node>();
            ret.Add(goal);
            while (iterator.parent != null)
            {
                ret.Add(iterator.parent);
                iterator = iterator.parent;
            }
            return ret;
        }

        private bool IsCorner(Vector2 tile)
        {
            Vector2[] neighbors = GetNeighbors(tile);
            bool[] cornersOccupied = new bool[4] {false, false, false, false};
            for (int i = 1; i < 8; i+=2)
            {
                Location loc = new Location((int) (neighbors[i].X * Game1.tileSize), (int) (neighbors[i].Y * Game1.tileSize));
                if (!gameLocation.isTileLocationOpen(loc) && neighbors[i] != negativeOne)
                {
                    Location n1 = new Location((int)(neighbors[i-1].X * Game1.tileSize), (int)(neighbors[i-1].Y * Game1.tileSize));
                    Location n2 = new Location((int) (neighbors[i + 1 > 7 ? 0 : i + 1].X * Game1.tileSize),
                        (int) (neighbors[i + 1 > 7 ? 0 : i + 1].Y * Game1.tileSize));
                    if ((gameLocation.isTileLocationOpen(n1) && neighbors[i - 1] != negativeOne) &&
                        (gameLocation.isTileLocationOpen(n2) && neighbors[i + 1 > 7 ? 0 : i + 1] != negativeOne))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private float ManhattanDistance(Vector2 a, Vector2 b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        private float EuclideanDistanceSquared(Vector2 a, Vector2 b)
        {
            return ((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y));
        }
    }

    class Node
    {
        public Node parent;
        public Vector2 position;
        public float f { get; set; }
        public float g { get; set; }
        public float h { get; set; }

        public Node(Node parent, Vector2 position)
        {
            this.parent = parent;
            this.position = position;
        }
    }

    class PriorityQueue
    {
        Node[] items;
        Dictionary<Vector2, int> positions;
        public int Count { get; private set; }

        public PriorityQueue(Node head)
        {
            items = new Node[100];
            items[0] = null;
            items[1] = head;
            Count = 1;

            positions = new Dictionary<Vector2, int>();
            positions.Add(head.position, 1);
        }

        public void Enqueue(Node item)
        {
            int position;
            if (!positions.TryGetValue(item.position, out position))
            {
                Count++;
                if (Count >= items.Length / 2)
                    ResizeItems();
                items[Count] = item;
                positions.Add(item.position, Count);
                PercolateUp(Count);
            }
            else if (items[position].f > item.f)
            {
                items[position] = item;
                PercolateUp(position);
            }
        }

        public Node Dequeue()
        {
            Node ret = items[1];
            items[1] = items[Count];
            items[Count] = null;
            positions.Remove(ret.position);
            if (Count > 1)
                positions[items[1].position] = 1;
            PercolateDown();
            Count--;
            return ret;
        }

        public Node Peek()
        {
            return items[1];
        }

        public float CheckValue(Vector2 nodeValue)
        {
            if (positions.TryGetValue(nodeValue, out int itemPosition))
                return items[itemPosition].f;
            return float.PositiveInfinity;
        }

        private void PercolateUp(int index)
        {
            while (items[index / 2] != null)
            {
                Node child = items[index];
                Node parent = items[index / 2];
                if (child.f < parent.f)
                {
                    items[index] = parent;
                    positions[parent.position] = index;
                    items[index / 2] = child;
                    positions[child.position] = index / 2;
                    index = index / 2;
                }
                else
                    return;
            }
        }

        private void PercolateDown()
        {
            int index = 1;
            int replaceIndex = 1;
            while (index == replaceIndex)
            {
                Node child_L = items[(index * 2)];
                Node child_R = items[(index * 2) + 1];
                Node parent = items[index];
                if (child_L != null && child_R != null)
                    replaceIndex = child_L.f < child_R.f ? (index * 2) : (index * 2) + 1;
                else if (child_L != null)
                    replaceIndex = index * 2;
                else
                    return;
                Node child = items[replaceIndex];
                if (child.f < parent.f)
                {
                    items[replaceIndex] = parent;
                    positions[parent.position] = replaceIndex;
                    items[index] = child;
                    positions[child.position] = index;
                    index = replaceIndex;
                }
            }
        }

        private void ResizeItems()
        {
            Node[] newItems = new Node[items.Length * 2];
            for (int i = 0; i < Count; i++)
                newItems[i] = items[i];
            items = newItems;
        }
    }
}
