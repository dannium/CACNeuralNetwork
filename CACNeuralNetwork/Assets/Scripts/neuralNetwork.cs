using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using Unity.VisualScripting;
using System.Threading;
using System.Xml;

public class neuralNetwork : MonoBehaviour
{
    [SerializeField] System.Random random;
    float destinationX = 4f;
    float destinationY = 12f;
    public float score = 0f;
    [SerializeField] int[] layers; //amount of neurons in each layer
    [SerializeField] float[][] neurons; //layer of neuron, specific neuron
    public float[][][] weights; //layer of weight, neuron weight affects, weight's value

    public int layerAmount; //number of layers in network (probably gonna be 4)
    public int[] neuronAmount; //number of nuerons in each layer

    public float bias; // starting weight for all weights

    public float mutateChance; // write as percent

    public int id;
    Rigidbody2D rb;
    bool foundPlayer = false;

    private void initNeurons()
    {
        neurons = new float[layerAmount][];
        for (int i = 0; i < layerAmount; i++)
        {
            neurons[i] = new float[neuronAmount[i]]; //creates an array of neurons for every layer
        }
    }

    private void initWeights()
    {
         UnityEngine.Random.InitState(id);
        weights = new float[layerAmount - 1][][];
        for (int i = 0; i < layerAmount - 1; i++) //create until layer before output layer
        {  
            weights[i] = new float[neuronAmount[i]][]; //creates array for weights coming from layer i
            for (int j = 0; j < neuronAmount[i]; j++)
            {
                weights[i][j] = new float[neuronAmount[i]]; //creates an array of weights based on the amount of neurons the weights come from

                for (int k = 0; k < neuronAmount[i]; k++)
                {
                    weights[i][j][k] = (float)random.NextDouble() - 0.5f;
                    print(i + ", " + j + ", " + k);
                }
            }
        }
    }

    private float[] inputs()
    {
        float[] array = new float[2]; //makes an array with the length being the amount of inputs (2 rn)
        array[0] = destinationX - transform.position.x;
        array[1] = destinationY - transform.position.y;
        return array;
    }

    private float[] outputs(float[] inputs)
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            neurons[0][i] = inputs[i]; //sets inputs to first layer neurons
        }

        for (int i = 1; i < layers.Length; i++) // Loop through each layer starting from the first hidden layer
        {
            for (int j = 0; j < neurons[i].Length - 1; j++) // Loop through each neuron in the current layer
            {
                float value = 0.25f;
                for (int k = 0; k < neurons[i-1].Length; k++) // Loop through each neuron in the previous layer
                {
                    value += weights[i - 1][j][k] * neurons[i - 1][k];
                }
                neurons[i][j] = (float)Math.Tanh(value); // Set value of current layer neuron between -1 and 1
            }
        }

        return neurons[neurons.Length - 1];
    }

    public GameObject createChild(int id)
    {
        score = 0;
        GameObject Child = Instantiate(transform.gameObject);
        neuralNetwork nn = Child.GetComponent<neuralNetwork>(); //gets child's neural network script
        Child.name = id.ToString();
        nn.score = 0;
        nn.id = this.id;
        nn.random = new System.Random();
        nn.layers = new int[layerAmount];
        nn.layerAmount = layerAmount;
        nn.neuronAmount = neuronAmount;
        nn.bias = bias;
        nn.destinationX = destinationX;
        nn.destinationY = destinationY;
        nn.mutateChance = mutateChance;
        for (int i = 0; i < layerAmount; i++)
        {
            nn.layers[i] = neuronAmount[i]; // Set each layer to the corresponding number of neurons
        }

        //initalize neurons and weights
        nn.initNeurons();
        nn.initWeights();

        //Child.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = id.ToString();
        for (int i = 0; i < layerAmount - 1; i++) //create until layer before output layer
        {
            nn.weights[i] = new float[neuronAmount[i]][]; //creates array for weights coming from layer i
            for (int j = 0; j < neuronAmount[i]; j++)
            {
                nn.weights[i][j] = new float[neuronAmount[i + 1]]; //creates an array of weights based on the amount of neurons the weights come from

                for (int k = 0; k < neuronAmount[i + 1]; k++)
                {
                    if (mutateChance < UnityEngine.Random.Range(1, 100))
                    {
                        weights[i][j][k] = (float)random.NextDouble() - 0.5f;
                        //(float)random.NextDouble() - 0.5f; //mutates (changes) weight to new random num
                    }
                    else
                    {
                        nn.weights[i][j][k] = weights[i][j][k]; //sets weight to parent's weight
                    }
                }
            }
        }

        return Child;
    }


    // Start is called before the first frame update
    void Start()
    {
        //transform.GetComponentInChildren<TextMeshProUGUI>().text = name.ToString();
        destinationX = GameObject.FindGameObjectWithTag("plr").transform.position.x;
        destinationY = GameObject.FindGameObjectWithTag("plr").transform.position.y;
        random = new System.Random(); // Initialize the random variable        
        layers = new int[layerAmount];
        for (int i = 0; i < layerAmount; i++)
        {
            layers[i] = neuronAmount[i]; // Set each layer to the corresponding number of neurons
        }

        //initalize neurons and weights
        initNeurons();
        initWeights();
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!foundPlayer)
        {
            rb.MovePosition(new Vector2(gameObject.transform.position.x + outputs(inputs())[0] * Time.deltaTime * 20, transform.position.y + outputs(inputs())[1] * Time.deltaTime * 20)); //changes bots position based on outputs
        }
        if(name == "17")
        {
            //print("ix:" + inputs()[0] + ", iy: " + inputs()[1] + ", ox: " + outputs(inputs())[0] + ", oy: " + outputs(inputs())[1]);
        }
        // score += outputs(inputs())[0];
        //        score += outputs(inputs())[1];
        score -= (transform.position.y - destinationY) * Time.deltaTime;
        score -= (transform.position.x - destinationX)  * Time.deltaTime;

    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if(col.gameObject.tag == "wall")
        {
            score -= 25f * Time.deltaTime;
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.tag == "plr")
        {
            score += 10000;
            foundPlayer = true;
        }
    }
}
