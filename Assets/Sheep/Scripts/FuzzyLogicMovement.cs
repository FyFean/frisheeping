using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;


public static class GlobalsMovement
{
    public static int N_SAMPLES = 10;
}

public enum CharacteristicM
{
    Extraversion,
    Adventurous,
    Agreeableness,
    Noise,
    DogRepulsion,
    SheepRepulsion
}

public enum CharacteristicArrayM
{
    NearbySheep,
    Direction,
}

public enum DecisionModelM
{
    Noise,
    DogRepulsion,
    SheepRepulsion
}

public class RuleMovement
{
    public CharacteristicM[] input_characteristic;
    public CharacteristicM[] array_characteristic;
    public string[] input_values;
    public DecisionModelM output_decisions;
    public string output_values;
    public string type;
    public float weight;
    public int N_SAMPLES;

    public RuleMovement(string type, CharacteristicM[] characteristic, string[] input_values, DecisionModelM dm, string output_values, float weight)
    {
        this.N_SAMPLES = GlobalsMovement.N_SAMPLES;
        this.type = type;
        this.input_characteristic = characteristic;
        this.input_values = input_values;
        this.output_decisions = dm;
        this.output_values = output_values;
        this.weight = weight;
    }

    public float[] evaluateRule(Dictionary<CharacteristicM, float> Input)
    {
        int in_len = input_values.Length;
        float[] fuzzified_val = new float[in_len];

        for (int i = 0; i < in_len; i++)
        {
            CharacteristicM charr = input_characteristic[i];
            string single_val = input_values[i];
            float input_val = Input[charr];
            fuzzified_val[i] = fuzzifyValue(charr, input_val, single_val);
        }

        float fuzzyFinal = -1f;

        switch (this.type)
        {
            case "":
                fuzzyFinal = fuzzified_val[0];
                break;
            case "min":
                fuzzyFinal = fuzzified_val.Min();
                break;
            case "max":
                fuzzyFinal = fuzzified_val.Max();
                break;
        }

        float[] sampled = SampleFunction(output_decisions, fuzzyFinal, output_values);
        return sampled;
    }

    public float fuzzifyValue(CharacteristicM characteristic, float value, string single_val)
    {
        switch (characteristic)
        {
            case CharacteristicM.Adventurous:
                switch (single_val)
                {
                    case "positive":
                        return TriangularMembership(value, 0, 30, 60);
                    case "negative":
                        return TriangularMembership(value, 40, 70, 100);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case CharacteristicM.Agreeableness:
                switch (single_val)
                {
                    case "positive":
                        return SigmoidalMembership(value, 10, 10);
                    case "negative":
                        return SigmoidalMembership(value, 10, 10);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case CharacteristicM.Extraversion:
                switch (single_val)
                {
                    case "positive":
                        return TriangularMembership(value, 0, 20, 40);
                    case "negative":
                        return TriangularMembership(value, 10, 60, 100);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case CharacteristicM.DogRepulsion:
                switch (single_val)
                {
                    case "positive":
                        return TrapezoidalMembership(value, 0, 20, 40, 60);
                    case "negative":
                        return TrapezoidalMembership(value, 40, 60, 80, 100);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case CharacteristicM.SheepRepulsion:
                switch (single_val)
                {
                    case "positive":
                        return TrapezoidalMembership(value, 0, 20, 40, 60);
                    case "negative":
                        return TrapezoidalMembership(value, 40, 60, 80, 100);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case CharacteristicM.Noise:
                switch (single_val)
                {
                    case "positive":
                        return TrapezoidalMembership(value, 0, 20, 40, 60);
                    case "negative":
                        return TrapezoidalMembership(value, 40, 60, 80, 100);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            default:
                throw new ArgumentException($"Unsupported characteristic: {characteristic}");
        }
    }

    float[] SampleFunction(DecisionModelM characteristic, float value, string single_val)
    {

        float minValue = 0f;
        float maxValue = 1f;
        float interval = (maxValue - minValue) / (-1);
        float[] sampled = new float[this.N_SAMPLES];

        switch (characteristic)
        {
            case DecisionModelM.DogRepulsion:
                switch (single_val)
                {
                    case "positive":
                        for (int i = 0; i < this.N_SAMPLES; i++)
                        {
                            //xValues[i] = minValue + i * interval;
                            float v = Mathf.Min(value, TrapezoidalMembership(i, 0, 20, 40, 60));
                            sampled[i] = v;
                        }

                        return sampled;
                    case "negative":
                        for (int i = 0; i < this.N_SAMPLES; i++)
                        {
                            //xValues[i] = minValue + i * interval;
                            float v = Mathf.Min(value, TrapezoidalMembership(i, 0, 20, 40, 60));
                            sampled[i] = v;
                        }

                        return sampled;
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case DecisionModelM.Noise:
                switch (single_val)
                {
                    case "positive":
                        for (int i = 0; i < this.N_SAMPLES; i++)
                        {
                            //xValues[i] = minValue + i * interval;
                            float v = Mathf.Min(value, TrapezoidalMembership(i, 0, 10, 60, 70));
                            sampled[i] = v;
                        }

                        return sampled;
                    case "negative":
                        for (int i = 0; i < this.N_SAMPLES; i++)
                        {
                            //xValues[i] = minValue + i * interval;
                            float v = Mathf.Min(value, TrapezoidalMembership(i, 0, 50, 90, 100));
                            sampled[i] = v;
                        }

                        return sampled;
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case DecisionModelM.SheepRepulsion:
                switch (single_val)
                {
                    case "positive":
                        for (int i = 0; i < this.N_SAMPLES; i++)
                        {
                            //xValues[i] = minValue + i * interval;
                            float v = Mathf.Min(value, TrapezoidalMembership(i, 0, 20, 40, 60));
                            sampled[i] = v;
                        }

                        return sampled;
                    case "negative":
                        for (int i = 0; i < this.N_SAMPLES; i++)
                        {
                            //xValues[i] = minValue + i * interval;
                            float v = Mathf.Min(value, TrapezoidalMembership(i, 0, 20, 40, 60));
                            sampled[i] = v;
                        }

                        return sampled;
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            default:
                throw new ArgumentException($"Unsupported characteristic: {characteristic}");
                //    xValues[i] = minValue + i * interval;
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

public class FuzzyLogicMovement
{
    private Dictionary<CharacteristicM, float> FuzzyInput;
    private RuleMovement[] rules;
    private int N_SAMPLES;

    public FuzzyLogicMovement()
    {
        N_SAMPLES = GlobalsMovement.N_SAMPLES;
        this.FuzzyInput = new Dictionary<CharacteristicM, float>();
        foreach (CharacteristicM characteristic in Enum.GetValues(typeof(CharacteristicM)))
        {
            float randomValue = (float)Random.Range(0, 101);
            this.FuzzyInput.Add(characteristic, randomValue);
        }
        this.FuzzyInput[CharacteristicM.DogRepulsion] = 5f;
        this.FuzzyInput[CharacteristicM.Noise] = 5f;
        this.FuzzyInput[CharacteristicM.SheepRepulsion] = 5f;


        // TODO: NOAH PRAVILA
        //if extraversion + and agreeableness + then n neighbors high
        //if adventurous + and agreeableness - then n neighbors low
        //if adventurous + then noise high
        //if adventurous + then dog repulsion low
        this.rules = new RuleMovement[]
        {
            new RuleMovement("min", new CharacteristicM[] {CharacteristicM.Adventurous, CharacteristicM.Agreeableness}, new string[] {"positive", "positive"}, DecisionModelM.SheepRepulsion, "negative", 1.0f),
            new RuleMovement("min", new CharacteristicM[] {CharacteristicM.Adventurous }, new string[] { "positive"}, DecisionModelM.DogRepulsion, "positive", 1.0f),
            new RuleMovement("min", new CharacteristicM[] {CharacteristicM.SheepRepulsion, CharacteristicM.Agreeableness }, new string[] { "positive", "negative"}, DecisionModelM.SheepRepulsion, "negative", 1.0f),
            new RuleMovement("min", new CharacteristicM[] {CharacteristicM.Adventurous }, new string[] { "negative"}, DecisionModelM.DogRepulsion, "negative", 1.0f),
            new RuleMovement("min", new CharacteristicM[] {CharacteristicM.Adventurous }, new string[] { "negative"}, DecisionModelM.Noise, "negative", 1.0f),
            new RuleMovement("min", new CharacteristicM[] {CharacteristicM.Adventurous }, new string[] { "positive"}, DecisionModelM.Noise, "positive", 1.0f)
        };
    }


    public float[] fuzzyfy()
    {
        DecisionModelM[] allValues = (DecisionModelM[])Enum.GetValues(typeof(DecisionModelM));
        Dictionary<DecisionModelM, Dictionary<string, float[]>> outputDict = new Dictionary<DecisionModelM, Dictionary<string, float[]>>();
        Dictionary<DecisionModelM, Dictionary<string, float>> countDict = new Dictionary<DecisionModelM, Dictionary<string, float>>();
        Dictionary<DecisionModelM, float[]> finalDict = new Dictionary<DecisionModelM, float[]>();


        foreach (DecisionModelM value in allValues)
        {
            Dictionary<string, float[]> innerDict = new Dictionary<string, float[]>
            {
                { "positive", new float[this.N_SAMPLES]},
                { "negative", new float[this.N_SAMPLES]}
            };
            Dictionary<string, float> innerCountDict = new Dictionary<string, float>
            {
                { "positive", 0f},
                { "negative", 0f}
            };
            outputDict.Add(value, innerDict);
            countDict.Add(value, innerCountDict);
        }

        for (int i = 0; i < this.rules.Length; i++)
        {
            RuleMovement curr_rule = this.rules[i];
            float[] f_val = curr_rule.evaluateRule(this.FuzzyInput);
            for (int j = 0; j < f_val.Length; j++)
            {
                outputDict[curr_rule.output_decisions][curr_rule.output_values][j] += f_val[j];
            }
            countDict[curr_rule.output_decisions][curr_rule.output_values] += 1.0f;
        }

        // calculate average
        foreach (DecisionModelM decision in outputDict.Keys)
        {
            Dictionary<string, float[]> innerOutputDict = outputDict[decision];
            Dictionary<string, float> innerCountDict = countDict[decision];

            foreach (string outputKey in innerOutputDict.Keys.ToList())
            {
                if (innerCountDict[outputKey] > 0)
                {
                    for (int i = 0; i < innerOutputDict[outputKey].Length; i++)
                    {
                        innerOutputDict[outputKey][i] /= innerCountDict[outputKey];
                    }
                }
            }

            float[] finalValues = new float[this.N_SAMPLES];
            float max_val = 0.0f;

            for (int i = 0; i < this.N_SAMPLES; i++)
            {
                foreach (string outputKey in innerOutputDict.Keys.ToList())
                {
                    float curr_val = innerOutputDict[outputKey][i];
                    if (curr_val > max_val)
                    {
                        max_val = curr_val;
                    }

                }
                finalValues[i] = max_val;
            }
            //Debug.Log(string.Join(", ", finalValues));
            finalDict.Add(decision, finalValues);
        }

        //TODO: preveri, ce dela ok... verjetno treba se kaj dodati
        float[] centroids = DefuzzifyCentroids(finalDict);

        return centroids;
    }

    // Combine fuzzy outputs using the centroid method (defuzzification)
    float[] DefuzzifyCentroids(Dictionary<DecisionModelM, float[]> fuzzySets)
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
    float CalculateCentroid(float[] fuzzySet)
    {
        float numerator = 0.0f;
        float denominator = 0.0f;

        foreach (var val in fuzzySet)
        {
            numerator += val;
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
