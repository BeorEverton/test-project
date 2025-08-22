using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.WaveSystem
{
    public class EnemyManager : MonoBehaviour
    {
        private bool _gameRunning;

        private readonly List<GameObject> _pendingRemovals = new List<GameObject>();

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
            var enemies = EnemySpawner.Instance.EnemiesAlive;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemyGO = enemies[i];
                if (enemyGO == null)
                {
                    _pendingRemovals.Add(enemyGO);
                    continue;
                }

                Enemy enemyComponent = enemyGO.GetComponent<Enemy>();
                if (enemyComponent == null || !enemyComponent.IsAlive)
                {
                    _pendingRemovals.Add(enemyGO);
                    continue;
                }

                // Knockback handling
                if (enemyComponent.KnockbackTime > 0f)
                {
                    if (enemyComponent.IsBossInstance)
                    {
                        enemyComponent.KnockbackVelocity /= 2f;
                        enemyComponent.KnockbackTime /= 2f;
                    }

                    enemyComponent.KnockbackVelocity.x = 0f; // Ensure no lateral movement
                    enemyComponent.transform.position +=
                        (Vector3)(enemyComponent.KnockbackVelocity * Time.deltaTime);

                    enemyComponent.KnockbackTime -= Time.deltaTime;
                    continue; // Skip movement/attack while being pushed
                }

                // Movement & grid position
                if (!enemyComponent.CanAttack || enemyComponent.KnockbackTime > 0f)
                {
                    MoveEnemy(enemyComponent);
                    HandleGridPosition(enemyGO, enemyComponent);
                }
                else
                {
                    // Within attack range
                    if (enemyComponent.transform.position.Depth() <= enemyComponent.attackRange)
                    {
                        // Check for trap in current cell
                        if (TrapPoolManager.Instance.hasAnyTrapActive)
                        {
                            Trap trap = TrapPoolManager.Instance.GetTrapAtCell(enemyComponent.LastGridPos);
                            if (trap != null)
                                StartCoroutine(TriggerTrapCoroutine(trap, enemyComponent));
                        }

                        TryAttack(enemyComponent);
                    }
                    else
                    {
                        // Cancel attack state if pushed out of range
                        enemyComponent.CanAttack = false;
                    }
                }
            }

            // Remove any dead/null enemies in one pass
            if (_pendingRemovals.Count > 0)
            {
                for (int r = 0; r < _pendingRemovals.Count; r++)
                {
                    enemies.Remove(_pendingRemovals[r]);
                }
                _pendingRemovals.Clear();
            }
        }


        private void MoveEnemy(Enemy enemy)
        {
            GameObject enemyGameObject = enemy.gameObject;

            enemy.transform.position += enemy.MoveDirection * enemy.MovementSpeed * Time.deltaTime;

            if (enemyGameObject.transform.position.Depth() <= enemy.attackRange)
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

            if (TrapPoolManager.Instance.hasAnyTrapActive)
            {
                Trap trap = TrapPoolManager.Instance.GetTrapAtCell(currentGridPos);
                if (trap != null)
                    StartCoroutine(TriggerTrapCoroutine(trap, enemyComponent));
            }
        }

        private IEnumerator TriggerTrapCoroutine(Trap trap, Enemy enemyComponent)
        {
            if (trap.delay > 0f)
                yield return new WaitForSeconds(trap.delay);

            if (trap.radius <= 0f)
            {
                // Single target
                trap.Trigger(enemyComponent);

            }
            else
            {
                // AOE
                var enemies = GridManager.Instance.GetEnemiesInRange(
                    GridManager.Instance.GetWorldPosition(trap.cell, trap.worldY),
                    Mathf.CeilToInt(trap.radius));
                Vector3 trapWorldPos = GridManager.Instance.GetWorldPosition(trap.cell, trap.worldY);

                foreach (var e in enemies)
                {
                    if (e != null && Vector3.Distance(e.transform.position, trapWorldPos) <= trap.radius)
                        trap.Trigger(e);
                }
            }
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