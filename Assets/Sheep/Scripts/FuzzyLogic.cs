using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;


public static class Globals
{
    public static int N_SAMPLES = 10;
}

public enum Characteristic
{
    Extraversion,
    Adventurous,
    Agreeableness,
    Noise,
    DogRepulsion,
    SheepRepulsion
}

public enum DecisionModel
{
    Noise,
    DogRepulsion,
    SheepRepulsion
}

public class Rule
{
    public Characteristic[] input_characteristic;
    public string[] input_values;
    public DecisionModel output_decisions;
    public string output_values;
    public string type;
    public float weight;
    public int N_SAMPLES;

    public Rule(string type, Characteristic[] characteristic, string[] input_values, DecisionModel dm, string output_values, float weight)
    {
        this.N_SAMPLES = Globals.N_SAMPLES;
        this.type = type;
        this.input_characteristic = characteristic;
        this.input_values = input_values;
        this.output_decisions = dm;
        this.output_values = output_values;
        this.weight = weight;
    }

    public float[] evaluateRule(Dictionary<Characteristic, float> Input)
    {
        int in_len = input_values.Length;
        float[] fuzzified_val = new float[in_len];
        // 1. fuzzify inputs
        for (int i = 0; i < in_len; i++)
        {
            Characteristic charr = input_characteristic[i];
            string single_val = input_values[i];
            float input_val = Input[charr];
            fuzzified_val[i] = fuzzifyValue(charr, input_val, single_val);
        }

        float fuzzyFinal = -1f;
        // 2a. fuzzy operation
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
        // 2b. sample output distribution from 2a value
        float[] sampled = SampleFunction(output_decisions, fuzzyFinal, output_values);
        return sampled;
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
            case Characteristic.DogRepulsion:
                switch (single_val)
                {
                    case "positive":
                        return TrapezoidalMembership(value, 0, 20, 40, 60);
                    case "negative":
                        return TrapezoidalMembership(value, 40, 60, 80, 100);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case Characteristic.SheepRepulsion:
                switch (single_val)
                {
                    case "positive":
                        return TrapezoidalMembership(value, 0, 20, 40, 60);
                    case "negative":
                        return TrapezoidalMembership(value, 40, 60, 80, 100);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case Characteristic.Noise:
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

    float[] SampleFunction(DecisionModel characteristic, float value, string single_val)
    {

        float minValue = 0f;
        float maxValue = 1f;
        float interval = (maxValue - minValue) / (-1);
        float[] sampled = new float[this.N_SAMPLES];

        switch (characteristic)
        {
            case DecisionModel.DogRepulsion:
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
            case DecisionModel.Noise:
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
            case DecisionModel.SheepRepulsion:
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

//https://www.mathworks.com/help/fuzzy/fuzzy-inference-process.html
//https://www.mathworks.com/help/fuzzy/mamdani_tipping_new.png
public class FuzzyLogic
{
    private Dictionary<Characteristic, float> FuzzyInput;
    private Rule[] rules;
    private int N_SAMPLES;

    public FuzzyLogic()
    {
        N_SAMPLES = Globals.N_SAMPLES;
        this.FuzzyInput = new Dictionary<Characteristic, float>();
        // initial crisp value of character (0 to 100) - value before fuzzification
        foreach (Characteristic characteristic in Enum.GetValues(typeof(Characteristic)))
        {
            float randomValue = (float)Random.Range(0, 101);
            this.FuzzyInput.Add(characteristic, randomValue);
        }
        // initial decision parameters
        this.FuzzyInput[Characteristic.DogRepulsion] = 5f;
        this.FuzzyInput[Characteristic.Noise] = 5f;
        this.FuzzyInput[Characteristic.SheepRepulsion] = 5f;

        // we define rules here
        {
            new Rule("min", new Characteristic[] {Characteristic.Adventurous, Characteristic.Agreeableness}, new string[] {"positive", "positive"}, DecisionModel.SheepRepulsion, "negative", 1.0f),
            new Rule("min", new Characteristic[] {Characteristic.Adventurous }, new string[] { "positive"}, DecisionModel.DogRepulsion, "positive", 1.0f),
            new Rule("min", new Characteristic[] {Characteristic.SheepRepulsion, Characteristic.Agreeableness }, new string[] { "positive", "negative"}, DecisionModel.SheepRepulsion, "negative", 1.0f),
            new Rule("min", new Characteristic[] {Characteristic.Adventurous }, new string[] { "negative"}, DecisionModel.DogRepulsion, "negative", 1.0f),
            new Rule("min", new Characteristic[] {Characteristic.Adventurous }, new string[] { "negative"}, DecisionModel.Noise, "negative", 1.0f),
            new Rule("min", new Characteristic[] {Characteristic.Adventurous }, new string[] { "positive"}, DecisionModel.Noise, "positive", 1.0f)
        };
    }


    public float[] fuzzyfy()
    {
        // init all
        DecisionModel[] allValues = (DecisionModel[])Enum.GetValues(typeof(DecisionModel));
        Dictionary<DecisionModel, Dictionary<string, float[]>> outputDict = new Dictionary<DecisionModel, Dictionary<string, float[]>>();
        Dictionary<DecisionModel, Dictionary<string, float>> countDict = new Dictionary<DecisionModel, Dictionary<string, float>>();
        Dictionary<DecisionModel, float[]> finalDict = new Dictionary<DecisionModel, float[]>();


        foreach (DecisionModel value in allValues)
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

        //1. and 2. and 3. (https://www.mathworks.com/help/fuzzy/mamdani_tipping_new.png)
        for (int i = 0; i < this.rules.Length; i++)
        {
            Rule curr_rule = this.rules[i];
            float[] f_val = curr_rule.evaluateRule(this.FuzzyInput);
            for (int j = 0; j < f_val.Length; j++)
            {
                outputDict[curr_rule.output_decisions][curr_rule.output_values][j] += f_val[j];
            }
            countDict[curr_rule.output_decisions][curr_rule.output_values] += 1.0f;
        }

        foreach (DecisionModel decision in outputDict.Keys)
        {
            Dictionary<string, float[]> innerOutputDict = outputDict[decision];
            Dictionary<string, float> innerCountDict = countDict[decision];

            // calculate average if we have multiple same output + value (pos/neg/...)
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

            // 4. (https://www.mathworks.com/help/fuzzy/mamdani_tipping_new.png)
            // aggregate by max for values (pos/neg/...) of an output
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
        // 5. Deffuzify via centroids
        float[] centroids = DefuzzifyCentroids(finalDict);

        return centroids;
    }

    // Combine fuzzy outputs using the centroid method (defuzzification)
    float[] DefuzzifyCentroids(Dictionary<DecisionModel, float[]> fuzzySets)
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
