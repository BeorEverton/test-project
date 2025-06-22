using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using System;
using UnityEngine;

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
                if (!_gameRunning)
                    break;
                Enemy enemyComponent = enemy.GetComponent<Enemy>();
                if (!enemyComponent.IsAlive)
                    continue;

                if (enemyComponent.KnockbackTime > 0f)
                {
                    if (enemyComponent.IsBossInstance)
                    {
                        enemyComponent.KnockbackVelocity /= 2f;
                        enemyComponent.KnockbackTime /= 2f;
                    }

                    enemy.transform.position += (Vector3)(enemyComponent.KnockbackVelocity * Time.deltaTime);
                    enemyComponent.KnockbackTime -= Time.deltaTime;
                    continue; // Skip movement/attack while being pushed
                }

                if (!enemyComponent.CanAttack || enemyComponent.KnockbackTime > 0f)
                {
                    MoveEnemy(enemyComponent);
                    HandleGridPosition(enemy, enemyComponent);
                }
                else
                {
                    if (enemy.transform.position.y <= enemyComponent.Info.AttackRange)
                        TryAttack(enemyComponent);
                    else
                        enemyComponent.CanAttack = false; // Cancel attack state if pushed out of range

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
            if (enemy.KnockbackTime > 0f)
                return;

            enemy.TimeSinceLastAttack += Time.deltaTime;
            if (enemy.TimeSinceLastAttack < 1 / enemy.Info.AttackSpeed)
                return;

            enemy.AnimateAttack();
            Attack(enemy.Info.Damage);

            enemy.TimeSinceLastAttack = 0f;
        }

        private void Attack(float damage)
        {
            PlayerBaseManager.Instance.TakeDamage(damage);
        }
    }
}