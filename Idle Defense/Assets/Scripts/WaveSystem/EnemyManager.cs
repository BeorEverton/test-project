using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace Assets.Scripts.WaveSystem
{
    public class EnemyManager : MonoBehaviour
    {
        private bool _gameRunning;

        private void Start()
        {
            EnemySpawner.Instance.OnWaveStarted += OnWaveStarted;
            EnemySpawner.Instance.OnWaveCompleted += OnWaveStopped;
            PlayerBaseManager.Instance.OnWaveFailed += OnWaveStopped;
            Debug.Log($"EnemyManager is listening for waveStart");
        }

        private void OnDisable()
        {
            EnemySpawner.Instance.OnWaveStarted -= OnWaveStarted;
            EnemySpawner.Instance.OnWaveCompleted -= OnWaveStopped;
            PlayerBaseManager.Instance.OnWaveFailed -= OnWaveStopped;
        }

        private void OnWaveStopped(object sender, EventArgs e)
        {
            _gameRunning = false;
        }

        private void OnWaveStarted(object sender, EventArgs e)
        {
            _gameRunning = true;
        }

        private void Update()
        {
            if (!_gameRunning)
                return;

            HandleEnemiesAlive();
        }

        private void HandleEnemiesAlive()
        {
            foreach (GameObject enemy in EnemySpawner.Instance.EnemiesAlive)
            {
                Enemy enemyComponent = enemy.GetComponent<Enemy>();
                if (!enemyComponent.CanAttack)
                {
                    MoveEnemy(enemyComponent);
                    HandleGridPosition(enemy, enemyComponent);
                }
                else
                {
                    TryAttack(enemyComponent);
                }
            }
        }

        private void MoveEnemy(Enemy enemy)
        {
            GameObject enemyGameObject = enemy.gameObject;
            enemyGameObject.transform.position += Vector3.down * enemy.MovementSpeed * Time.deltaTime;

            if (enemyGameObject.transform.position.y <= enemy.Info.AttackRange)
                enemy.CanAttack = true;
        }

        private void HandleGridPosition(GameObject enemy, Enemy enemyComponent)
        {
            Vector2Int currentGridPos = GridManager.Instance.GetGridPosition(enemy.transform.position);

            if (currentGridPos == enemyComponent.LastGridPos)
                return;

            GridManager.Instance.RemoveEnemy(enemyComponent, enemyComponent.LastGridPos);
            GridManager.Instance.AddEnemy(enemyComponent);
            enemyComponent.LastGridPos = currentGridPos;
        }

        private void TryAttack(Enemy enemy)
        {
            enemy.TimeSinceLastAttack += Time.deltaTime;
            if (enemy.TimeSinceLastAttack < enemy.Info.AttackSpeed)
                return;

            Attack(enemy.Info.Damage);
            enemy.TimeSinceLastAttack = 0f;
        }

        private void Attack(float damage)
        {
            PlayerBaseManager.Instance.TakeDamage(damage);
        }
    }
}