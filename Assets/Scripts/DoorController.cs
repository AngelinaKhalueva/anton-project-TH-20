using UnityEngine;

namespace AntonDoors
{
    public enum DoorType { Normal, Trap, Exit }

    public class DoorController : MonoBehaviour
    {
        public DoorType Type;
        public Transform Hinge;
        public float OpenAngle = -105f;
        public float OpenDistance = 2.4f;
        public float CloseDistance = 3.2f;

        public bool IsOpen { get; private set; }

        private float openness;

        public void Open()
        {
            IsOpen = true;
        }

        public void Close()
        {
            if (Type == DoorType.Normal) IsOpen = false;
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State != GameManager.RunState.Playing || gm.Player == null) return;

            Vector3 playerPos = gm.Player.transform.position;
            float dist = Vector3.Distance(transform.position, playerPos);

            if (dist < OpenDistance) IsOpen = true;
            else if (Type == DoorType.Normal && dist > CloseDistance) IsOpen = false;

            float target = IsOpen ? 1f : 0f;
            openness = Mathf.MoveTowards(openness, target, Time.deltaTime * 3.5f);
            if (Hinge != null) Hinge.localRotation = Quaternion.Euler(0f, openness * OpenAngle, 0f);

            if (Type == DoorType.Exit && dist < 1.7f) gm.Win();
        }
    }
}
