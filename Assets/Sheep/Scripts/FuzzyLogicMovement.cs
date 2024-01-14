using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;


public enum CharacteristicMovement
{
    Heading,
    Speed,
    Significance
}

public enum DecisionModelMovement
{
    Speed,
    Heading
}

public class RuleMovement
{
    public CharacteristicMovement[] input_characteristic;
    public string[] input_values;
    public DecisionModelMovement output_decisions;
    public string output_values;
    public string type;
    public float weight;
    public int N_SAMPLES;

    public RuleMovement(string type, CharacteristicMovement[] characteristic, string[] input_values, DecisionModelMovement dm, string output_values, float weight)
    {
        this.N_SAMPLES = Globals.N_SAMPLES;
        this.type = type;
        this.input_characteristic = characteristic;
        this.input_values = input_values;
        this.output_decisions = dm;
        this.output_values = output_values;
        this.weight = weight;
    }

    public float[] evaluateRule(Dictionary<CharacteristicMovement, float> Input)
    {
        int in_len = input_values.Length;
        float[] fuzzified_val = new float[in_len];
        // 1. fuzzify inputs
        for (int i = 0; i < in_len; i++)
        {
            CharacteristicMovement charr = input_characteristic[i];
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

    public float fuzzifyValue(CharacteristicMovement characteristic, float value, string single_val)
    {
        switch (characteristic)
        {
            case CharacteristicMovement.Heading:
                switch (single_val)
                {
                    // heading left -> heading positive
                    // heading same -> heading negative
                    // heading right -> heading neutral 
                    case "positive":
                        //return TriangularMembership(value, 0, 90, 180);
                        return TriangularMembership(value, 0, 90, 180);
                    case "neutral":
                        //return TriangularMembership(value, 180, 270, 360);
                        return TriangularMembership(value, 180, 270, 360);
                    case "negative":    
                        //return TriangularMembership(value, 90, 180, 270);
                        return TriangularMembership(value, 90, 180, 270);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case CharacteristicMovement.Speed:
                // speed slow -> speed positive
                // speed same -> speed negative
                // speed fast -> speed neutral
                switch (single_val)
                {
                    case "positive":
                        return TriangularMembership(value, 0f, 0f, 1.5f);
                    case "neutral":
                        return TriangularMembership(value, 1.5f, 3f, 3f);
                    case "negative":
                        return TriangularMembership(value, 0f, 1.5f, 3f);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            case CharacteristicMovement.Significance:
                // significance high -> significance negative
                // significance low -> significance positive
                switch (single_val)
                {
                    case "positive":
                        return LinearMembership(value, 0, 1);
                    //return TrapezoidalMembership(value, 0, 20, 40, 60);
                    case "neutral":
                        return -1.0f;
                    case "negative":
                        return LinearMembership(value, 1, 0);
                    //return TrapezoidalMembership(value, 40, 60, 80, 100);
                    default:
                        throw new ArgumentException($"Unsupported characteristic: {single_val}");
                }
            default:
                throw new ArgumentException($"Unsupported characteristic: {characteristic}");
        }
    }

    float[] SampleFunction(DecisionModelMovement characteristic, float value, string single_val)
    {

        float[] sampled = new float[this.N_SAMPLES];
        switch (characteristic)
        {
            case DecisionModelMovement.Heading:
                {
                    float minValue = 0.0f;
                    float maxValue = 360.0f;
                    float interval = (maxValue - minValue) / (N_SAMPLES - 1);
                    switch (single_val)
                    {
                        case "positive":
                            for (int i = 0; i < this.N_SAMPLES; i++)
                            {
                                float x = minValue + i * interval;
                                float v = Mathf.Min(value, TriangularMembership(x, 0, 90, 180));
                                sampled[i] = v;
                            }
                            return sampled;
                        case "negative":
                            for (int i = 0; i < this.N_SAMPLES; i++)
                            {
                                float x = minValue + i * interval;
                                float v = Mathf.Min(value, TriangularMembership(x, 90, 180, 270));
                                sampled[i] = v;
                            }
                            return sampled;
                        case "neutral":
                            for (int i = 0; i < this.N_SAMPLES; i++)
                            {
                                float x = minValue + i * interval;
                                float v = Mathf.Min(value, TriangularMembership(x, 180, 270, 360));
                                sampled[i] = v;
                            }
                            return sampled;
                        default:
                            throw new ArgumentException($"Unsupported characteristic: {single_val}");
                    }
                }
            case DecisionModelMovement.Speed:
                {
                    float minValue = 0.0f;
                    float maxValue = 3.0f;
                    float interval = (maxValue - minValue) / (N_SAMPLES - 1);
                    switch (single_val)
                    {
                        case "positive":
                            for (int i = 0; i < this.N_SAMPLES; i++)
                            {
                                float x = minValue + i * interval;
                                //xValues[i] = minValue + i * interval;
                                float v = Mathf.Min(value, TriangularMembership(x, 0f, 0f, 1.5f));
                                sampled[i] = v;
                            }

                            return sampled;
                        case "neutral":
                            for (int i = 0; i < this.N_SAMPLES; i++)
                            {
                                float x = minValue + i * interval;
                                float v = Mathf.Min(value, TriangularMembership(x, 1.5f, 3f, 3f));
                                sampled[i] = v;
                            }
                            return sampled;
                        case "negative":
                            for (int i = 0; i < this.N_SAMPLES; i++)
                            {
                                float x = minValue + i * interval;
                                float v = Mathf.Min(value, TriangularMembership(x, 0f, 1.5f, 3f));
                                sampled[i] = v;
                            }

                            return sampled;

                        default:
                            throw new ArgumentException($"Unsupported characteristic: {single_val}");
                    }
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

    static float LinearMembership(float x, float a, float b)
    {
        if (x <= a) return 0.0f;
        if (a < x && x <= b) return (x - a) / (b - a);
        return 1.0f;
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
public class FuzzyLogicMovement
{
    private Dictionary<CharacteristicMovement, float> FuzzyInput;
    private RuleMovement[] rules;
    private int N_SAMPLES;
    private int sheep_id;

    public FuzzyLogicMovement(int sheep_id)
    {
        this.sheep_id = sheep_id;
        N_SAMPLES = Globals.N_SAMPLES;
        this.FuzzyInput = new Dictionary<CharacteristicMovement, float>();

        // we define rules here
        this.rules = new RuleMovement[]
        {
            // heading left -> heading positive
            // heading same -> heading negative
            // heading right -> heading neutral 
            // speed slow -> speed positive
            // speed same -> speed negative
            // speed fast -> speed neutral
            // significance high -> significance negative
            // significance low -> significance positive
            
            new RuleMovement("min", new CharacteristicMovement[] {CharacteristicMovement.Heading, CharacteristicMovement.Significance }, new string[] { "positive", "negative"}, DecisionModelMovement.Heading, "positive", 1.0f),
            new RuleMovement("max", new CharacteristicMovement[] {CharacteristicMovement.Heading, CharacteristicMovement.Significance}, new string[] {"neutral", "negative"}, DecisionModelMovement.Heading, "neutral", 1.0f),
            new RuleMovement("max", new CharacteristicMovement[] {CharacteristicMovement.Heading, CharacteristicMovement.Significance }, new string[] { "negative", "positive"}, DecisionModelMovement.Heading, "negative", 1.0f),
            new RuleMovement("max", new CharacteristicMovement[] {CharacteristicMovement.Speed, CharacteristicMovement.Significance }, new string[] { "positive", "negative"}, DecisionModelMovement.Speed, "positive", 0.0f),
            new RuleMovement("max", new CharacteristicMovement[] {CharacteristicMovement.Speed, CharacteristicMovement.Significance  }, new string[] { "neutral", "negative"}, DecisionModelMovement.Speed, "neutral", 0.0f),
            new RuleMovement("max", new CharacteristicMovement[] {CharacteristicMovement.Speed, CharacteristicMovement.Significance }, new string[] { "negative", "positive"}, DecisionModelMovement.Speed, "negative", 0.0f),
        };
    }

    float CalculateAverage(float[] array)
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


    float ScaleSheepPosValue(float value)
    {
        if (value <= 5)
        {
            return 1.0f;
        }
        else if (value > 5 && value <= 60)
        {
            return 1.0f - (value - 5) / 55.0f;
        }
        else
        {
            return 0.0f;
        }
    }

    float MapValueToRange(float originalValue, float originalMin, float originalMax, float newMin, float newMax)
    {
        // Check if the original value is above the maximum allowed
        if (originalValue > originalMax)
        {
            return newMax;
        }

        // Linear mapping formula
        float mappedValue = ((originalValue - originalMin) / (originalMax - originalMin)) * (newMax - newMin) + newMin;
        return mappedValue;
    }
    public float[] fuzzyfy(float currTheta, float[] SheepPos, float[] DogPos, float[] sheepAng, float[] sheepSpeed)
    {
        //SheepPos = new float[] { SheepPos[0] };
        //sheepAng = new float[] { sheepAng[0] += 180 };
        //sheepSpeed = new float[] { sheepSpeed[0] };
        //if (this.sheep_id == 1)
        //{
        //    Debug.Log("sheepssssssss (" + SheepPos.Length + "): " + string.Join(", ", SheepPos));
        //    Debug.Log("sheepssssssss (" + DogPos.Length + "): " + string.Join(", ", DogPos));

        //}
        // change model by current state of the env.
        //float avg_dog_dist = this.CalculateAverage(DogPos);
        ////float avg_sheep_dist = this.CalculateAverage(SheepPos);
        //float basic_range1 = this.MapValueToRange(avg_dog_dist, 0.0f, 5.0f, 0.0f, 2.0f);

        //float big_range1 = this.MapValueToRange(avg_dog_dist, 0.0f, 10.0f, -50.0f, 50.0f);
        //float basic_range2 = this.MapValueToRange(avg_sheep_dist, 0.0f, 5.0f, -10.0f, 10.0f);
        //float big_range2 = this.MapValueToRange(avg_sheep_dist, 0.0f, 10.0f, -50.0f, 50.0f);
        ////Debug.Log("Razdalce Razdalce " + avg_dog_dist + " " + avg_sheep_dist);
        ////Debug.Log("Razdalce Izpeljave1 " + basic_range1 + " " + big_range1);
        ////Debug.Log("Razdalce Izpeljave2 " + basic_range2 + " " + big_range2);
        //AddCharacteristicMovementVal(CurrentInputs, CharacteristicMovement.SheepRepulsion, big_range2);

        // init all

        DecisionModelMovement[] allValues = (DecisionModelMovement[])Enum.GetValues(typeof(DecisionModelMovement));
        Dictionary<DecisionModelMovement, Dictionary<string, float[]>> outputDict = new Dictionary<DecisionModelMovement, Dictionary<string, float[]>>();
        Dictionary<DecisionModelMovement, Dictionary<string, float>> countDict = new Dictionary<DecisionModelMovement, Dictionary<string, float>>();
        Dictionary<DecisionModelMovement, float[]> finalDict = new Dictionary<DecisionModelMovement, float[]>();


        foreach (DecisionModelMovement value in allValues)
        {
            Dictionary<string, float[]> innerDict = new Dictionary<string, float[]>
            {
                { "positive", new float[this.N_SAMPLES]},
                { "neutral", new float[this.N_SAMPLES]},
                { "negative", new float[this.N_SAMPLES]}
            };
            Dictionary<string, float> innerCountDict = new Dictionary<string, float>
            {
                { "positive", 0f},
                { "neutral", 0f},
                { "negative", 0f}
            };
            outputDict.Add(value, innerDict);
            countDict.Add(value, innerCountDict);
        }

        //1. and 2. and 3. (https://www.mathworks.com/help/fuzzy/mamdani_tipping_new.png)
        for (int v = 0; v < SheepPos.Length; v++)
        {
            float significance = ScaleSheepPosValue(SheepPos[v]);

            Dictionary<CharacteristicMovement, float> CurrentInputs = new Dictionary<CharacteristicMovement, float>();
            CurrentInputs.Add(CharacteristicMovement.Heading, sheepAng[v]);
            CurrentInputs.Add(CharacteristicMovement.Speed, sheepSpeed[v]);
            CurrentInputs.Add(CharacteristicMovement.Significance, significance);

            for (int i = 0; i < this.rules.Length; i++)
            {
                RuleMovement curr_rule = this.rules[i];
                float[] f_val = curr_rule.evaluateRule(CurrentInputs);
                //if (this.sheep_id == 15)
                //{

                //    Debug.Log("final test ***" + significance + "* " + sheepAng[v] + ", " + curr_rule.output_decisions + " " + curr_rule.output_values + ": " + string.Join(", ", f_val));
                //}
                for (int j = 0; j < f_val.Length; j++)
                {
                    outputDict[curr_rule.output_decisions][curr_rule.output_values][j] += f_val[j];
                }
                countDict[curr_rule.output_decisions][curr_rule.output_values] += 1.0f;
            }
        }

        foreach (DecisionModelMovement decision in outputDict.Keys)
        {
            Dictionary<string, float[]> innerOutputDict = outputDict[decision];
            Dictionary<string, float> innerCountDict = countDict[decision];

            // calculate average if we have multiple same output + value (pos/neg/...)
            foreach (string outputKey in innerOutputDict.Keys.ToList())
            {
                if (innerCountDict[outputKey] > 0)
                {
                    //if (this.sheep_id == 15)
                    //{
                    //    Debug.Log("final test *" + innerCountDict[outputKey] + "* " +);

                    //   Debug.Log("final test *" + innerCountDict[outputKey] + "* " + decision + ", " + outputKey + ": " + string.Join(", ", innerOutputDict[outputKey]));
                    //}
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

            if (this.sheep_id == 15)
            {
                Debug.Log("final test" + decision + ": " + string.Join(", ", finalValues));
            }
            finalDict.Add(decision, finalValues);
        }

        // 5. Deffuzify via centroids
        float[] centroids = CalcCentroids(finalDict);
        if (this.sheep_id == 15)
        {
            Debug.Log("final test: " + string.Join(", ", centroids));
        }


        return centroids;
    }

    // Combine fuzzy outputs using the centroid method (defuzzification)
    float[] CalcCentroids(Dictionary<DecisionModelMovement, float[]> fuzzySets)
    {
        List<float> centroids = new List<float>();
        float[,] parameterRanges = new float[,]
        {
            { 0.0f, 3.0f},
            { 0.0f, 360.0f}
        };
        int i = 0;

        foreach (var outerPair in fuzzySets)
        {
            float centroid = CalculateCentroid(outerPair.Value, parameterRanges[i, 0], parameterRanges[i, 1], this.N_SAMPLES);
            centroids.Add(centroid);
            i++;
        }

        return centroids.ToArray();
    }

    // Function to calculate centroid for a single fuzzy set
    float CalculateCentroid(float[] fuzzySet, float start, float end, float size)
    {
        float step = (end - start) / (size - 1);

        float numerator = 0.0f;
        float denominator = 0.0f;
        int i = 0;

        foreach (var val in fuzzySet)
        {
            numerator += val * (start + i * step);
            denominator += val;//(start + i * step);
            //if (this.sheep_id == 15)
            //{
            //    Debug.Log(val + " " + (start + i * step) + " " + val * (start + i * step));
            //}
            i++;
        }


        // Handle division by zero
        if (denominator == 0.0f)
        {
            return 0.0f;
        }

        return numerator / denominator;
    }
}

