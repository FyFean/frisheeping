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

    public static Enums.SheepState FloatToSpeedEnum(float val)
    {

        if (val <= 1f)
        {
            return Enums.SheepState.idle;
        }
        else if (val <= 2f)
        {
            return Enums.SheepState.walking;
        }
        else if (val <= 3f)
        {
            return Enums.SheepState.running;
        }
        return Enums.SheepState.idle;

    }

    public static float SpeedEnumtoFloat(Enums.SheepState val)
    {
        switch (val)
        {
            case Enums.SheepState.idle:
                return 0.5f;
            case Enums.SheepState.walking:
                return 1.5f;
            case Enums.SheepState.running:
                return 2.5f;
        }
        return -1.0f;
    }

    public static float[] GetSheepSpeeds(float val, IEnumerable<SheepController> sheepNeighbours)
    {
        List<float> positions = new List<float>();

        foreach (var sheep in sheepNeighbours)
        {
            float v = SheepUtils.SpeedEnumtoFloat(sheep.sheepState);
            positions.Add(v);
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
            Vector2 point2D = new Vector2(point.x, point.z);
            Vector2 arrayPoint2D = new Vector2(pointsArray[i].x, pointsArray[i].z);

            // Calculate the distance using Vector2.Distance
            distances[i] = Vector2.Distance(point2D, arrayPoint2D);
        }

        return distances;
    }

    public static float CalculateAverage(float[] array)
    {
        if (array == null || array.Length == 0)
        {
            return 0.0f;
        }

        float sum = 0.0f;

        foreach (float number in array)
        {
            sum += number;
        }

        return sum / array.Length;
    }

    public static float[] CalculateAngles(Transform currentPosition, IEnumerable<SheepController> neighborTransforms)
    {
        Vector3 currentDirection = currentPosition.forward;
        List<float> angles = new List<float>();

        foreach (var sheep in neighborTransforms)
        {
            //Transform neighborTransform = sheep.transform;
            ////Vector3 neighborDirection = (neighborTransform.position - currentPosition.position).normalized;
            //float angle = Vector3.Angle(currentDirection, neighborTransform.forward);
            //Vector3 crossProduct = Vector3.Cross(currentDirection, neighborTransform.forward);
            //float sign = Mathf.Sign(Vector3.Dot(Vector3.up, crossProduct));

            //float signedAngle = angle * sign;
            //float signedAngle = (sheep.getTheta() + 180) % 360;
            float signedAngle = sheep.getTheta();
            angles.Add(signedAngle);
        }

        return angles.ToArray();
    }
}