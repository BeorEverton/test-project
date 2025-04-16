using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class GameSpeedManager : MonoBehaviour
    {
        public void SetGameSpeed(int speed)
        {
            Time.timeScale = speed;
        }
    }
}