using System.Collections.Generic;
using UnityEngine;
using Maze = AntonDoors.MazeGenerator.Maze;
using MazeDir = AntonDoors.MazeGenerator.Dir;

namespace AntonDoors
{
    public static class WorldBuilder
    {
        public const float CellSize = 6f;
        private const float WallHeight = 3.2f;
        private const float WallThickness = 0.3f;
        private const int GridW = 7;
        private const int GridH = 7;
        private const int TrapCount = 3;

        public static GameManager CreateWorld(int seed)
        {
            if (seed == 0) seed = Random.Range(1, 1000000);
            Maze maze = MazeGenerator.Generate(GridW, GridH, seed);

            GameObject rootGo = new GameObject("GameRoot");
            Transform root = rootGo.transform;

            SetupRenderSettings();

            Material wallMat = MakeMaterial(new Color(0.13f, 0.14f, 0.17f));
            Material floorMat = MakeMaterial(new Color(0.075f, 0.075f, 0.095f));
            Material ceilMat = MakeMaterial(new Color(0.045f, 0.045f, 0.065f));
            Material frameMat = MakeMaterial(new Color(0.26f, 0.26f, 0.3f));
            Material exitDoorMat = MakeMaterial(new Color(0.06f, 0.55f, 0.3f));
            Material monsterMat = MakeMaterial(new Color(0.55f, 0.03f, 0.05f), true, new Color(1f, 0.08f, 0.08f));
            Material eyeMat = MakeMaterial(Color.white, true, new Color(2f, 2f, 2f));
            Font font = GetFont();

            BuildEnvironment(root, maze, floorMat, ceilMat, wallMat);
            BuildFillLight(root);

            GameObject gmGo = new GameObject("GameManager");
            gmGo.transform.SetParent(root, false);
            GameManager gm = gmGo.AddComponent<GameManager>();

            List<Vector2Int> deadEnds = maze.DeadEnds();
            int[] dist = maze.DistancesFrom(0, 0);

            // Выход — самый далёкий тупик от старта.
            Vector2Int exitCell = deadEnds[0];
            int best = -1;
            foreach (Vector2Int c in deadEnds)
            {
                int d = dist[c.y * GridW + c.x];
                if (d > best) { best = d; exitCell = c; }
            }
            deadEnds.Remove(exitCell);
            Shuffle(deadEnds);

            // Монстры стартуют в случайных тупиках, дальше ищут игрока сами.
            var monsterCells = new List<Vector2Int>();
            foreach (Vector2Int c in deadEnds)
            {
                if (monsterCells.Count >= TrapCount) break;
                if (c.x == 0 && c.y == 0) continue;
                monsterCells.Add(c);
            }
            while (monsterCells.Count < TrapCount)
            {
                Vector2Int c = new Vector2Int(Random.Range(0, GridW), Random.Range(0, GridH));
                if ((c.x == 0 && c.y == 0) || c == exitCell || monsterCells.Contains(c)) continue;
                monsterCells.Add(c);
            }

            gm.Maze = maze;
            MazeDir exitDir = maze.SingleOpenDir(exitCell.x, exitCell.y);
            CreateDoor(root, exitCell, exitDir, DoorType.Exit, exitDoorMat, frameMat);

            PlayerController player = BuildPlayer(root);
            BuildMonsters(root, gm, monsterCells, monsterMat, eyeMat);
            BuildRoomLights(root, exitCell, monsterCells);

            HudController hud = BuildHud(root, font);
            gm.Initialize(player, hud);

            return gm;
        }

        private static void SetupRenderSettings()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.045f, 0.05f, 0.075f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.02f);
            RenderSettings.fogDensity = 0.055f;
        }

        private static void BuildEnvironment(Transform root, Maze maze, Material floorMat, Material ceilMat, Material wallMat)
        {
            Vector3 center = new Vector3((GridW - 1) * CellSize * 0.5f, 0f, (GridH - 1) * CellSize * 0.5f);
            CreateCube(root, "Floor", center + Vector3.down * 0.25f,
                new Vector3(GridW * CellSize + 2f, 0.5f, GridH * CellSize + 2f), floorMat);
            CreateCube(root, "Ceiling", center + Vector3.up * (WallHeight + 0.25f),
                new Vector3(GridW * CellSize + 2f, 0.5f, GridH * CellSize + 2f), ceilMat);
            BuildWalls(root, maze, wallMat);
        }

        private static void BuildWalls(Transform root, Maze maze, Material wallMat)
        {
            for (int z = 0; z < GridH; z++)
            {
                for (int x = 0; x < GridW; x++)
                {
                    if (!maze.Has(x, z, MazeDir.N)) CreateWall(root, x, z, MazeDir.N, wallMat);
                    if (!maze.Has(x, z, MazeDir.W)) CreateWall(root, x, z, MazeDir.W, wallMat);
                    if (z == 0 && !maze.Has(x, z, MazeDir.S)) CreateWall(root, x, z, MazeDir.S, wallMat);
                    if (x == GridW - 1 && !maze.Has(x, z, MazeDir.E)) CreateWall(root, x, z, MazeDir.E, wallMat);
                }
            }
        }

        private static void CreateWall(Transform root, int x, int z, MazeDir dir, Material mat)
        {
            Vector3 c = CellCenter(x, z) + MazeGenerator.DirVector(dir) * (CellSize * 0.5f);
            c.y = WallHeight * 0.5f;
            bool alongX = (dir == MazeDir.N || dir == MazeDir.S);
            Vector3 scale = alongX
                ? new Vector3(CellSize + WallThickness, WallHeight, WallThickness)
                : new Vector3(WallThickness, WallHeight, CellSize + WallThickness);
            CreateCube(root, "Wall", c, scale, mat);
        }

        private static void BuildFillLight(Transform root)
        {
            GameObject go = new GameObject("FillLight");
            go.transform.SetParent(root, false);
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.12f;
            light.color = new Color(0.55f, 0.6f, 0.9f);
            light.shadows = LightShadows.None;
        }

        private static PlayerController BuildPlayer(Transform root)
        {
            GameObject go = new GameObject("Player");
            go.transform.SetParent(root, false);
            go.transform.position = CellCenter(0, 0) + Vector3.up * 0.9f;

            CharacterController cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = Vector3.zero;
            cc.stepOffset = 0.35f;

            GameObject camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(go.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);

            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            camGo.AddComponent<AudioListener>();

            Light flashlight = camGo.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.range = 22f;
            flashlight.spotAngle = 62f;
            flashlight.intensity = 3.2f;
            flashlight.color = new Color(1f, 0.95f, 0.82f);
            flashlight.shadows = LightShadows.Soft;

            PlayerController player = go.AddComponent<PlayerController>();
            player.ViewCamera = cam;
            return player;
        }

        private static void BuildMonsters(Transform root, GameManager gm, List<Vector2Int> cells, Material bodyMat, Material eyeMat)
        {
            foreach (Vector2Int cell in cells)
            {
                Vector3 center = CellCenter(cell.x, cell.y);
                gm.MonsterSpawns.Add(center);

                GameObject go = new GameObject("Monster");
                go.transform.SetParent(root, false);
                go.transform.position = center + Vector3.up * 1.0f;

                GameObject body = CreatePrimitive(root, PrimitiveType.Capsule, "Body",
                    go.transform.position, new Vector3(0.95f, 1f, 0.95f), bodyMat);
                body.transform.SetParent(go.transform, true);

                GameObject eyeL = CreatePrimitive(root, PrimitiveType.Sphere, "EyeL",
                    go.transform.position + new Vector3(-0.22f, 0.45f, 0.42f), Vector3.one * 0.17f, eyeMat);
                eyeL.transform.SetParent(go.transform, true);
                GameObject eyeR = CreatePrimitive(root, PrimitiveType.Sphere, "EyeR",
                    go.transform.position + new Vector3(0.22f, 0.45f, 0.42f), Vector3.one * 0.17f, eyeMat);
                eyeR.transform.SetParent(go.transform, true);

                Light glow = go.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = new Color(1f, 0.1f, 0.1f);
                glow.range = 7f;
                glow.intensity = 1.8f;
                glow.shadows = LightShadows.None;

                // Монстр не должен физически блокировать игрока в коридоре.
                body.GetComponent<Collider>().isTrigger = true;
                eyeL.GetComponent<Collider>().isTrigger = true;
                eyeR.GetComponent<Collider>().isTrigger = true;

                MonsterController mc = go.AddComponent<MonsterController>();
                mc.Glow = glow;
                mc.Visuals = new Renderer[]
                {
                    body.GetComponent<Renderer>(),
                    eyeL.GetComponent<Renderer>(),
                    eyeR.GetComponent<Renderer>()
                };
            }
        }

        private static void BuildRoomLights(Transform root, Vector2Int exitCell, List<Vector2Int> trapCells)
        {
            CreatePointLight(root, CellCenter(exitCell.x, exitCell.y) + Vector3.up * 2.4f,
                new Color(0.2f, 1f, 0.5f), 8f, 1.6f);
            foreach (Vector2Int c in trapCells)
            {
                CreatePointLight(root, CellCenter(c.x, c.y) + Vector3.up * 2.5f,
                    new Color(1f, 0.12f, 0.12f), 7f, 1.1f);
            }
        }

        private static HudController BuildHud(Transform root, Font font)
        {
            GameObject go = new GameObject("HUD");
            go.transform.SetParent(root, false);
            HudController hud = go.AddComponent<HudController>();
            hud.Build(font);
            return hud;
        }

        private static void CreateDoor(Transform root, Vector2Int cell, MazeDir dir, DoorType type,
            Material doorMat, Material frameMat)
        {
            Vector3 center = CellCenter(cell.x, cell.y) + MazeGenerator.DirVector(dir) * (CellSize * 0.5f);
            float yaw = (dir == MazeDir.N || dir == MazeDir.S) ? 0f : 90f;
            const float width = 2.6f;

            GameObject group = new GameObject("Door_" + type);
            group.transform.SetParent(root, false);
            group.transform.position = center;
            group.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            GameObject hinge = new GameObject("Hinge");
            hinge.transform.SetParent(group.transform, false);
            hinge.transform.localPosition = new Vector3(-width * 0.5f, 0f, 0f);

            GameObject panel = CreateCube(hinge.transform, "Panel", Vector3.zero,
                new Vector3(width, 2.6f, 0.18f), doorMat);
            panel.transform.localPosition = new Vector3(width * 0.5f, 1.3f, 0f);

            CreateCube(group.transform, "FrameL", new Vector3(-width * 0.5f - 0.12f, 1.45f, 0f),
                new Vector3(0.24f, 2.9f, 0.38f), frameMat);
            CreateCube(group.transform, "FrameR", new Vector3(width * 0.5f + 0.12f, 1.45f, 0f),
                new Vector3(0.24f, 2.9f, 0.38f), frameMat);
            CreateCube(group.transform, "FrameTop", new Vector3(0f, 2.88f, 0f),
                new Vector3(width + 0.6f, 0.22f, 0.38f), frameMat);

            DoorController door = group.AddComponent<DoorController>();
            door.Type = type;
            door.Hinge = hinge.transform;
        }

        private static void CreatePointLight(Transform root, Vector3 pos, Color color, float range, float intensity)
        {
            GameObject go = new GameObject("RoomLight");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
        }

        private static GameObject CreateCube(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject CreatePrimitive(Transform root, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static Vector3 CellCenter(int x, int z)
        {
            return new Vector3(x * CellSize, 0f, z * CellSize);
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = Random.Range(i, list.Count);
                T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }

        private static Material MakeMaterial(Color color, bool emissive = false, Color emission = default(Color))
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            Material m = new Material(shader);
            m.color = color;
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
            }
            return m;
        }

        private static Font GetFont()
        {
            // Системный Arial содержит кириллицу; встроенный шрифт Unity — только латиницу.
            Font f = null;
            try { f = Font.CreateDynamicFontFromOSFont("Arial", 24); } catch { }
            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }
            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            }
            return f;
        }
    }
}
