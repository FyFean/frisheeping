using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Characteristic
{
    Extraversion,
    Adventurous,
    Agreeableness,
    // other characteristics...
}

public enum DecisionModel
{
    NeighborDistance,
    // other characteristics...
}

public class Rule
{
    Characteristic[] input_characteristic;
    string[] input_values;
    DecisionModel output_decisions;
    string output_values;
    string type;
    float weight;

    public Rule(string type, Characteristic[] characteristic, string[] input_values, DecisionModel dm, string output_values, float weight)
    {
        this.type = type;
        this.input_characteristic = characteristic;
        this.input_values = input_values;
        this.output_decisions = dm;
        this.output_values = output_values;
        this.weight = weight;
    }

    public float evaluateRule(Dictionary<Characteristic, float> Input)
    {
        int in_len = input_values.Length;
        float[] fuzzified_val = new float[in_len];

        for (int i = 0; i < in_len; i++)
        {
            Characteristic charr = input_characteristic[i];
            string single_val = input_values[i];
            float input_val = Input[charr];
            fuzzified_val[i] = fuzzifyValue(charr, input_val, single_val);
        }

        switch (this.type)
        {
            case "":
                return fuzzified_val[0];

            case "min":
                return fuzzified_val.Min();

            case "max":
                return fuzzified_val.Max();
        }

        return -1f;
    }

    public float fuzzifyValue(Characteristic characteristic, float value, string single_val)
    {
        switch (characteristic)
        {
            case Characteristic.Adventurous:
                switch (single_val)
                {
                    case "positive":
                        return ServicePoor(value);
                    case "negative":
                        return ServicePoor(value);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case Characteristic.Agreeableness:
                switch (single_val)
                {
                    case "positive":
                        return FoodDelicious(value);
                    case "negative":
                        return FoodDelicious(value);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case Characteristic.Extraversion:
                switch (single_val)
                {
                    case "positive":
                        return FoodDelicious(value);
                    case "negative":
                        return FoodDelicious(value);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            default:
                throw new ArgumentException($"Unsupported characteristic: {characteristic}");
        }
    }

    // Fuzzy membership functions
    private float ServicePoor(float service)
    {
        // Implement the membership function for poor service
        return 0f; // Placeholder value
    }

    private float FoodDelicious(float food)
    {
        // Implement the membership function for delicious food
        return 0f; // Placeholder value
    }
}

public class FuzzyLogic
{
    private Dictionary<Characteristic, float> FuzzyInput;
    private Rule[] rules;

    public FuzzyLogic()
    {
        FuzzyInput = new Dictionary<Characteristic, float>();
        FuzzyInput.Add(Characteristic.Extraversion, 10f);
        FuzzyInput.Add(Characteristic.Adventurous, 10f);
        FuzzyInput.Add(Characteristic.Agreeableness, 10f);

        rules = new Rule[]
        {
            new Rule("min", new Characteristic[] {Characteristic.Adventurous, Characteristic.Agreeableness}, new string[] {"positive", "negative"}, DecisionModel.NeighborDistance, "cheap", 1.0f),
            new Rule("min", new Characteristic[] {Characteristic.Adventurous, Characteristic.Agreeableness}, new string[] { "positive", "negative"}, DecisionModel.NeighborDistance, "cheap", 1.0f),
            new Rule("min", new Characteristic[] {Characteristic.Adventurous, Characteristic.Agreeableness}, new string[] { "positive", "negative"}, DecisionModel.NeighborDistance, "cheap", 1.0f)
        };

        float[] centroids = CalculateCentroids

    }

    // Combine fuzzy outputs using the centroid method (defuzzification)
    float[] CalculateCentroids(Dictionary<DecisionModel, Dictionary<string, float>> fuzzySets)
    {
        List<float> centroids = new List<float>();

        foreach (var outerPair in fuzzySets)
        {
            float centroid = CalculateCentroid(outerPair.Value);
            centroids.Add(centroid);
        }

        return centroids.ToArray();
    }

    // Function to calculate centroid for a single fuzzy set
    float CalculateCentroid(Dictionary<string, float> fuzzySet)
    {
        float numerator = 0.0f;
        float denominator = 0.0f;

        foreach (var innerPair in fuzzySet)
        {
            float grade = innerPair.Value;
            float value = Convert.ToSingle(innerPair.Key); // Assuming the keys are convertible to float

            numerator += value * grade;
            denominator += 1.0f; // Assuming equal weight for all values
        }

        if (denominator == 0.0f)
        {
            // Handle division by zero
            return 0.0f;
        }

        return numerator / denominator;
    }
}
