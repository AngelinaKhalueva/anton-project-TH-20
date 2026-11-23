using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntonDoors
{
    public static class MazeGenerator
    {
        [Flags]
        public enum Dir { None = 0, N = 1, E = 2, S = 4, W = 8 }

        public static readonly Dir[] AllDirs = { Dir.N, Dir.E, Dir.S, Dir.W };

        public static Dir Opposite(Dir d)
        {
            switch (d)
            {
                case Dir.N: return Dir.S;
                case Dir.E: return Dir.W;
                case Dir.S: return Dir.N;
                case Dir.W: return Dir.E;
                default: return Dir.None;
            }
        }

        public static Vector2Int Step(int x, int z, Dir d)
        {
            switch (d)
            {
                case Dir.N: return new Vector2Int(x, z + 1);
                case Dir.E: return new Vector2Int(x + 1, z);
                case Dir.S: return new Vector2Int(x, z - 1);
                case Dir.W: return new Vector2Int(x - 1, z);
                default: return new Vector2Int(x, z);
            }
        }

        public static Vector3 DirVector(Dir d)
        {
            switch (d)
            {
                case Dir.N: return Vector3.forward;
                case Dir.E: return Vector3.right;
                case Dir.S: return Vector3.back;
                case Dir.W: return Vector3.left;
                default: return Vector3.zero;
            }
        }

        [System.Serializable]
        public sealed class Maze
        {
            public int Width;
            public int Height;
            public Dir[] Cells;

            public Dir Get(int x, int z) { return Cells[z * Width + x]; }
            public void Set(int x, int z, Dir d) { Cells[z * Width + x] = d; }
            public bool Has(int x, int z, Dir d) { return (Get(x, z) & d) != 0; }

            public void Add(int x, int z, Dir from, Dir to)
            {
                Set(x, z, (Dir)((int)Get(x, z) | (int)from));
                Vector2Int n = Step(x, z, from);
                Set(n.x, n.y, (Dir)((int)Get(n.x, n.y) | (int)to));
            }

            public int Degree(int x, int z)
            {
                Dir d = Get(x, z);
                int c = 0;
                if ((d & Dir.N) != 0) c++;
                if ((d & Dir.E) != 0) c++;
                if ((d & Dir.S) != 0) c++;
                if ((d & Dir.W) != 0) c++;
                return c;
            }

            public List<Vector2Int> DeadEnds()
            {
                var list = new List<Vector2Int>();
                for (int z = 0; z < Height; z++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (Degree(x, z) == 1) list.Add(new Vector2Int(x, z));
                    }
                }
                return list;
            }

            public Dir SingleOpenDir(int x, int z)
            {
                Dir d = Get(x, z);
                foreach (Dir dir in AllDirs)
                {
                    if ((d & dir) != 0) return dir;
                }
                return Dir.N;
            }

            public int[] DistancesFrom(int sx, int sz)
            {
                int n = Width * Height;
                int[] dist = new int[n];
                for (int i = 0; i < n; i++) dist[i] = -1;

                var queue = new Queue<Vector2Int>();
                dist[sz * Width + sx] = 0;
                queue.Enqueue(new Vector2Int(sx, sz));

                while (queue.Count > 0)
                {
                    Vector2Int c = queue.Dequeue();
                    int cd = dist[c.y * Width + c.x];
                    foreach (Dir d in AllDirs)
                    {
                        if (!Has(c.x, c.y, d)) continue;
                        Vector2Int next = Step(c.x, c.y, d);
                        if (dist[next.y * Width + next.x] != -1) continue;
                        dist[next.y * Width + next.x] = cd + 1;
                        queue.Enqueue(next);
                    }
                }
                return dist;
            }
        }

        public static Maze Generate(int width, int height, int seed)
        {
            var rng = new System.Random(seed);
            var maze = new Maze { Width = width, Height = height, Cells = new Dir[width * height] };
            bool[] visited = new bool[width * height];
            var stack = new Stack<Vector2Int>();

            visited[0] = true;
            stack.Push(new Vector2Int(0, 0));

            while (stack.Count > 0)
            {
                Vector2Int cur = stack.Peek();
                var options = new List<Dir>();
                foreach (Dir d in AllDirs)
                {
                    Vector2Int n = Step(cur.x, cur.y, d);
                    if (n.x < 0 || n.y < 0 || n.x >= width || n.y >= height) continue;
                    if (!visited[n.y * width + n.x]) options.Add(d);
                }

                if (options.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                Dir chosen = options[rng.Next(options.Count)];
                Vector2Int next = Step(cur.x, cur.y, chosen);
                maze.Add(cur.x, cur.y, chosen, Opposite(chosen));
                visited[next.y * width + next.x] = true;
                stack.Push(next);
            }

            return maze;
        }
    }
}
