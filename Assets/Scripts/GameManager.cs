using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntonDoors
{
    public class GameManager : MonoBehaviour
    {
        public enum RunState { Playing, Won, Lost }

        public static GameManager Instance { get; private set; }

        public PlayerController Player;
        public HudController Hud;
        public MazeGenerator.Maze Maze;
        public float TimeLimit = 180f;

        // Точки, где монстры начинают свой обход лабиринта.
        public readonly List<Vector3> MonsterSpawns = new List<Vector3>();

        public RunState State { get; private set; }
        public int HP { get; private set; }

        private float timeLeft;
        private int maxHP = 3;
        private bool initialized;

        private void Awake()
        {
            Instance = this;
        }

        public void Initialize(PlayerController player, HudController hud)
        {
            Player = player;
            Hud = hud;
            ResetRun();
        }

        private void Start()
        {
            if (!initialized) Initialize(Player, Hud);
        }

        public void ResetRun()
        {
            initialized = true;
            HP = maxHP;
            timeLeft = TimeLimit;
            State = RunState.Playing;

            if (Hud != null)
            {
                Hud.HideEnd();
                Hud.SetHp(HP, maxHP);
                Hud.SetTime(timeLeft, TimeLimit);
                Hud.SetPrompt(string.Empty);
            }
        }

        private void Update()
        {
            if (State != RunState.Playing)
            {
                if (Input.GetKeyDown(KeyCode.R)) Restart();
                return;
            }

            timeLeft -= Time.deltaTime;
            if (Hud != null) Hud.SetTime(timeLeft, TimeLimit);
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                Lose("Время вышло!");
            }
        }

        public void TakeDamage(Vector3 fromDirection)
        {
            if (State != RunState.Playing) return;

            HP--;
            if (Hud != null)
            {
                Hud.SetHp(HP, maxHP);
                Hud.FlashDamage();
                Hud.ShowMessage("−1 HP", new Color(1f, 0.2f, 0.2f));
            }
            if (Player != null) Player.ApplyKnockback(fromDirection);

            if (HP <= 0) Lose("Монстр догнал тебя...");
        }

        public void Win()
        {
            if (State != RunState.Playing) return;
            State = RunState.Won;
            if (Hud != null) Hud.ShowEnd("ПОБЕДА!", "Ты нашёл правильную дверь.", true);
        }

        public void Lose(string reason)
        {
            if (State != RunState.Playing) return;
            State = RunState.Lost;
            if (Hud != null) Hud.ShowEnd("ПОРАЖЕНИЕ", reason, false);
        }

        private void Restart()
        {
            Scene active = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(active.path)) SceneManager.LoadScene(active.path);
            else if (!string.IsNullOrEmpty(active.name)) SceneManager.LoadScene(active.name);
            else SceneManager.LoadScene(0);
        }
    }
}
