using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public enum Characteristic
{
    Extraversion,
    Adventurous,
    Agreeableness,
    Angle,
    Speed,
}

public enum DecisionModel
{
    NeighborDistance,
    Angle,
    Speed,
}

public class Rule
{
    public Characteristic[] input_characteristic;
    public string[] input_values;
    public DecisionModel output_decisions;
    public string output_values;
    public string type;
    public float weight;

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
                        return TriangularMembership(value, 0, 30, 60);
                    case "negative":
                        return TriangularMembership(value, 40, 70, 100);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case Characteristic.Agreeableness:
                switch (single_val)
                {
                    case "positive":
                        return SigmoidalMembership(value, 10, 10);
                    case "negative":
                        return SigmoidalMembership(value, 10, 10);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case Characteristic.Extraversion:
                switch (single_val)
                {
                    case "positive":
                        return TriangularMembership(value, 0, 20, 40);
                    case "negative":
                        return TriangularMembership(value, 10, 60, 100);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case Characteristic.Speed:
                switch (single_val)
                {
                    case "positive":
                        return TriangularMembership(value, 0, 20, 40);
                    case "negative":
                        return TriangularMembership(value, 10, 60, 100);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case Characteristic.Angle:
                switch (single_val)
                {
                    case "positive":
                        return TriangularMembership(value, 0, 20, 40);
                    case "negative":
                        return TriangularMembership(value, 10, 60, 100);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            default:
                throw new ArgumentException($"Unsupported characteristic: {characteristic}");
        }
    }

    float TriangularMembership(float x, float a, float b, float c)
    {
        if (x <= a || x > c) return 0.0f;
        if (a < x && x <= b) return (x - a) / (b - a);
        if (b < x && x <= c) return (c - x) / (c - b);
        return 0.0f;
    }

    float TrapezoidalMembership(float x, float a, float b, float c, float d)
    {
        if (x <= a || x > d) return 0.0f;
        if (a < x && x <= b) return (x - a) / (b - a);
        if (b < x && x <= c) return 1.0f;
        if (c < x && x <= d) return (d - x) / (d - c);
        return 0.0f;
    }

    float GaussianMembership(float x, float c, float sigma)
    {
        return Mathf.Exp(-0.5f * Mathf.Pow((x - c) / sigma, 2));
    }

    float SigmoidalMembership(float x, float c, float alpha)
    {
        return 1.0f / (1.0f + Mathf.Exp(-alpha * (x - c)));
    }
}

public class FuzzyLogic
{
    private Dictionary<Characteristic, float> FuzzyInput;
    private Rule[] rules;

    public FuzzyLogic()
    {
        this.FuzzyInput = new Dictionary<Characteristic, float>();
        foreach (Characteristic characteristic in Enum.GetValues(typeof(Characteristic)))
        {
            float randomValue = (float)Random.Range(0, 101);
            this.FuzzyInput.Add(characteristic, randomValue);
        }


        // TODO: NOAH PRAVILA
        this.rules = new Rule[]
        {
            new Rule("min", new Characteristic[] {Characteristic.Adventurous, Characteristic.Agreeableness}, new string[] {"positive", "negative"}, DecisionModel.NeighborDistance, "positive", 1.0f),
            new Rule("min", new Characteristic[] {Characteristic.Adventurous, Characteristic.Agreeableness}, new string[] { "positive", "negative"}, DecisionModel.Angle, "positive", 1.0f),
            new Rule("min", new Characteristic[] {Characteristic.Adventurous, Characteristic.Agreeableness}, new string[] { "positive", "negative"}, DecisionModel.NeighborDistance, "negative", 1.0f)
        };
    }


    public float[] fuzzyfy(float angle, float speed)
    {
        this.FuzzyInput[Characteristic.Angle] = angle;
        this.FuzzyInput[Characteristic.Speed] = speed;
        DecisionModel[] allValues = (DecisionModel[])Enum.GetValues(typeof(DecisionModel));
        Dictionary<DecisionModel, Dictionary<string, float>> outputDict = new Dictionary<DecisionModel, Dictionary<string, float>>();
        Dictionary<DecisionModel, Dictionary<string, float>> countDict = new Dictionary<DecisionModel, Dictionary<string, float>>();

        foreach (DecisionModel value in allValues)
        {
            Dictionary<string, float> innerDict = new Dictionary<string, float>
            {
                { "positive", 0f },
                { "negative", 0f }
            };
            outputDict.Add(value, innerDict);
            countDict.Add(value, innerDict);
        }

        for (int i = 0; i < this.rules.Length; i++)
        {
            Rule curr_rule = this.rules[i];
            float f_val = curr_rule.evaluateRule(this.FuzzyInput);
            outputDict[curr_rule.output_decisions][curr_rule.output_values] += f_val;
            countDict[curr_rule.output_decisions][curr_rule.output_values] += 1.0f;
            // TODO: fuzzyfy output
        }

        // calculate average
        foreach (DecisionModel decision in outputDict.Keys)
        {
            Dictionary<string, float> innerOutputDict = outputDict[decision];
            Dictionary<string, float> innerCountDict = countDict[decision];

            foreach (string outputKey in innerOutputDict.Keys.ToList())
            {
                if (innerCountDict[outputKey] > 0)
                {
                    innerOutputDict[outputKey] /= innerCountDict[outputKey];
                }
            }
        }

        //TODO: preveri, ce dela ok... verjetno treba se kaj dodati
        float[] centroids = DefuzzifyCentroids(outputDict);

        return centroids;
    }

    // Combine fuzzy outputs using the centroid method (defuzzification)
    float[] DefuzzifyCentroids(Dictionary<DecisionModel, Dictionary<string, float>> fuzzySets)
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

            numerator += grade;
            denominator += 1.0f; // Assuming equal weight for all values
        }

        // Handle division by zero
        if (denominator == 0.0f)
        {
            return 0.0f;
        }

        return numerator / denominator;
    }
}
