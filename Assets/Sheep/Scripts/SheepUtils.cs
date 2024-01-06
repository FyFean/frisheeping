using UnityEngine;
using System.Collections.Generic;

public static class SheepUtils
{
    public static Vector3[] GetSheepPositions(IEnumerable<SheepController> sheepNeighbours)
    {
        List<Vector3> positions = new List<Vector3>();

        foreach (var sheep in sheepNeighbours)
        {
            positions.Add(sheep.transform.position);
        }

        return positions.ToArray();
    }

    public static Vector3[] GetDogPositions(IEnumerable<DogController> dogNeighbors)
    {
        List<Vector3> positions = new List<Vector3>();

        foreach (var dog in dogNeighbors)
        {
            positions.Add(dog.transform.position);
        }

        return positions.ToArray();
    }

    public static float[] CalculateDistances(Vector3 point, Vector3[] pointsArray)
    {
        float[] distances = new float[pointsArray.Length];

        for (int i = 0; i < pointsArray.Length; i++)
        {
            // Ignore the height component (z)
            Vector2 point2D = new Vector2(point.x, point.y);
            Vector2 arrayPoint2D = new Vector2(pointsArray[i].x, pointsArray[i].y);

            // Calculate the distance using Vector2.Distance
            distances[i] = Vector2.Distance(point2D, arrayPoint2D);
        }

        return distances;
    }
}