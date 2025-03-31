using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class GridRaycaster
    {
        public static List<Vector2Int> GetCellsAlongLine(Vector2 startPos, Vector2 targetPos, int maxSteps = 30)
        {
            List<Vector2Int> cells = new();

            Vector2 direction = (targetPos - startPos).normalized;
            Vector2 currentPos = startPos;
            Vector2Int currentCell = new(Mathf.FloorToInt(startPos.x), Mathf.FloorToInt(startPos.y));

            Vector2 deltaDist = new(
                Mathf.Abs(1f / direction.x),
                Mathf.Abs(1f / direction.y)
            );

            Vector2Int step = new(
                direction.x >= 0 ? 1 : -1,
                direction.y >= 0 ? 1 : -1
            );

            Vector2 rayLengths = new(
                (step.x > 0 ? (currentCell.x + 1 - currentPos.x) : (currentPos.x - currentCell.x)) * deltaDist.x,
                (step.y > 0 ? (currentCell.y + 1 - currentPos.y) : (currentPos.y - currentCell.y)) * deltaDist.y
            );

            for (int i = 0; i < maxSteps; i++)
            {
                cells.Add(currentCell);

                if (rayLengths.x < rayLengths.y)
                {
                    rayLengths.x += deltaDist.x;
                    currentCell.x += step.x;
                }
                else
                {
                    rayLengths.y += deltaDist.y;
                    currentCell.y += step.y;
                }
            }

            return cells;
        }
    }
}