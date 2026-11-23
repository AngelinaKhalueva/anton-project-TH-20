using System.Collections.Generic;
using UnityEngine;

namespace AntonDoors
{
    public class MonsterController : MonoBehaviour
    {
        private const float CellSize = WorldBuilder.CellSize;
        private const float AggroRange = 17f;
        private const float GiveUpTime = 4.5f;

        public float HuntSpeed = 3.3f;
        public float PatrolSpeed = 1.9f;
        public Renderer[] Visuals;
        public Light Glow;

        private readonly List<Vector2Int> path = new List<Vector2Int>();
        private Vector2Int patrolTarget;
        private Vector2Int lastPlayerCell = new Vector2Int(-1, -1);
        private int pathIndex;
        private float repathTimer;
        private float attackCooldown;
        private float lostTimer;
        private bool hunting;

        private void Start()
        {
            PickPatrolTarget();
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State != GameManager.RunState.Playing || gm.Player == null || gm.Maze == null) return;

            float dt = Time.deltaTime;
            Transform player = gm.Player.transform;
            float distToPlayer = Vector3.Distance(transform.position, player.position);

            bool seesPlayer = distToPlayer < AggroRange;
            if (seesPlayer)
            {
                lostTimer = 0f;
                if (!hunting && gm.Hud != null)
                    gm.Hud.ShowMessage("Монстр тебя заметил!", new Color(1f, 0.3f, 0.3f));
            }
            else
            {
                lostTimer += dt;
            }

            if (hunting && lostTimer > GiveUpTime)
            {
                TeleportAway();
                return;
            }
            hunting = seesPlayer;

            if (attackCooldown > 0f) attackCooldown -= dt;
            if (repathTimer > 0f) repathTimer -= dt;

            Vector2Int playerCell = CellOf(player.position);
            if (repathTimer <= 0f || playerCell != lastPlayerCell || pathIndex >= path.Count)
            {
                Repath(gm, CellOf(transform.position), playerCell);
                repathTimer = 0.4f;
                lastPlayerCell = playerCell;
            }

            Vector3 target;
            if (pathIndex < path.Count)
            {
                target = CellCenter(path[pathIndex]);
            }
            else if (hunting)
            {
                target = player.position;
            }
            else
            {
                target = CellCenter(patrolTarget);
            }

            Vector3 dir = target - transform.position;
            dir.y = 0f;
            float planarDist = dir.magnitude;
            if (planarDist < 0.3f)
            {
                if (pathIndex < path.Count) pathIndex++;
                else if (!hunting && Vector2.Distance(CellOf(transform.position), patrolTarget) < 1f) PickPatrolTarget();
            }
            else
            {
                float speed = hunting ? HuntSpeed : PatrolSpeed;
                transform.position += dir / planarDist * speed * dt;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 6f);
            }

            if (distToPlayer < 1.35f && attackCooldown <= 0f)
            {
                attackCooldown = 1.25f;
                gm.TakeDamage(player.position - transform.position);
            }
        }

        private void Repath(GameManager gm, Vector2Int from, Vector2Int playerCell)
        {
            path.Clear();
            pathIndex = 0;

            Vector2Int goal = hunting ? playerCell : patrolTarget;
            List<Vector2Int> cells = FindPath(gm.Maze, from, goal);
            if (cells != null) path.AddRange(cells);
        }

        private static List<Vector2Int> FindPath(MazeGenerator.Maze maze, Vector2Int from, Vector2Int to)
        {
            int w = maze.Width;
            int h = maze.Height;
            int total = w * h;
            int start = from.y * w + from.x;
            int finish = to.y * w + to.x;
            if (start < 0 || start >= total || finish < 0 || finish >= total) return null;
            if (start == finish) return new List<Vector2Int>();

            int[] prev = new int[total];
            for (int i = 0; i < total; i++) prev[i] = -1;
            var queue = new Queue<Vector2Int>();
            prev[start] = start;
            queue.Enqueue(from);

            while (queue.Count > 0 && prev[finish] == -1)
            {
                Vector2Int c = queue.Dequeue();
                foreach (MazeGenerator.Dir d in MazeGenerator.AllDirs)
                {
                    if (!maze.Has(c.x, c.y, d)) continue;
                    Vector2Int n = MazeGenerator.Step(c.x, c.y, d);
                    int idx = n.y * w + n.x;
                    if (prev[idx] != -1) continue;
                    prev[idx] = c.y * w + c.x;
                    queue.Enqueue(n);
                }
            }

            if (prev[finish] == -1) return null;

            var result = new List<Vector2Int>();
            int cur = finish;
            while (cur != start)
            {
                result.Add(new Vector2Int(cur % w, cur / w));
                cur = prev[cur];
            }
            result.Reverse();
            return result;
        }

        private void PickPatrolTarget()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.Maze == null) return;
            patrolTarget = new Vector2Int(Random.Range(0, gm.Maze.Width), Random.Range(0, gm.Maze.Height));
            path.Clear();
            pathIndex = 0;
            repathTimer = 0f;
        }

        private void TeleportAway()
        {
            GameManager gm = GameManager.Instance;
            List<Vector3> spawns = gm.MonsterSpawns;
            Vector3 target = spawns.Count > 0
                ? spawns[Random.Range(0, spawns.Count)]
                : new Vector3(0f, transform.position.y, 0f);

            transform.position = new Vector3(target.x, 1f, target.z);
            hunting = false;
            lostTimer = 0f;
            attackCooldown = 0f;
            PickPatrolTarget();
            if (gm.Hud != null) gm.Hud.ShowMessage("Монстр потерял след", new Color(0.4f, 1f, 0.7f));
        }

        private static Vector2Int CellOf(Vector3 position)
        {
            return new Vector2Int(Mathf.RoundToInt(position.x / CellSize), Mathf.RoundToInt(position.z / CellSize));
        }

        private static Vector3 CellCenter(Vector2Int cell)
        {
            return new Vector3(cell.x * CellSize, 1f, cell.y * CellSize);
        }
    }
}
