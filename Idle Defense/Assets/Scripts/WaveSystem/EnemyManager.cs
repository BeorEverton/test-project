using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using System;
using UnityEngine;

namespace Assets.Scripts.WaveSystem
{
    public class EnemyManager : MonoBehaviour
    {
        private void Update()
        {
            foreach (GameObject enemy in EnemySpawner.Instance.EnemiesAlive)
            {
                Enemy enemyComponent = enemy.GetComponent<Enemy>();
                if (!enemyComponent.CanAttack)
                {
                    MoveEnemy(enemyComponent);

                    Vector2Int currentGridPos = GridManager.Instance.GetGridPosition(enemy.transform.position);

                    if (currentGridPos != enemyComponent.LastGridPos)
                    {
                        GridManager.Instance.RemoveEnemy(enemyComponent, enemyComponent.LastGridPos);
                        GridManager.Instance.AddEnemy(enemyComponent);
                        enemyComponent.LastGridPos = currentGridPos;
                    }
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

        private void TryAttack(Enemy enemy)
        {
            enemy.TimeSinceLastAttack += Time.deltaTime;
            if (enemy.TimeSinceLastAttack < enemy.Info.AttackSpeed)
                return;

            Attack();
            enemy.TimeSinceLastAttack = 0f;
        }

        private void Attack()
        {
            Debug.Log("Attacking");
        }
    }
}