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
    float destinationX;
    float destinationY;
    public float score = 0f;
    [SerializeField] int[] layers; //amount of neurons in each layer
    [SerializeField] float[][] neurons; //layer of neuron, specific neuron
    public float[][][] weights; //layer of weight, neuron weight affects, weight's value

    public int layerAmount; //number of layers in network (probably gonna be 4)
    public int[] neuronAmount; //number of neurons in each layer

    public float bias; // starting weight for all weights

    public float mutateChance; // write as percent

    public int id;
    Rigidbody2D rb;
    bool foundPlayer = false;
    float moveSpeed = 5f; // Constant move speed for the bot

    // New variables for improved exploration
    private Vector2 lastPosition;
    private float stuckTime = 0f;
    private float stuckThreshold = 0.5f; // Reduced time before considering the bot stuck
    private float explorationTimer = 0f;
    private float explorationInterval = 3f; // Reduced time between random direction changes
    private Vector2 currentExplorationDirection;
    private float wallAvoidanceForce = 4f; // Increased force to apply when avoiding walls
    private float wallDetectionDistance = 1f; // Increased distance to check for walls
    private HashSet<Vector2Int> exploredCells = new HashSet<Vector2Int>();
    private float cellSize = 1f; // Size of each cell in the grid

    // New variables to prevent jittering and smooth movement
    private float minMovementThreshold = 0.01f;
    private float jitterPreventionTimer = 0f;
    private float jitterPreventionDuration = 0.5f;
    private Vector2 targetVelocity;
    private float smoothTime = 0.1f; // Time to smooth movement
    private Vector2 currentVelocity;

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
                weights[i][j] = new float[neuronAmount[i+1]]; //creates an array of weights based on the amount of neurons the weights come from

                for (int k = 0; k < neuronAmount[i+1]; k++)
                {
                    weights[i][j][k] = (float)random.NextDouble() - 0.5f;
                }
            }
        }
    }

    private float[] inputs()
    {
        float[] array = new float[neuronAmount[0]]; // Match input size to first layer neuron count
        array[0] = destinationX - transform.position.x;
        array[1] = destinationY - transform.position.y;
        if (neuronAmount[0] > 2)
        {
            array[2] = currentExplorationDirection.x;
            array[3] = currentExplorationDirection.y;
        }
        return array;
    }

    private float[] outputs(float[] inputs)
    {
        if (inputs.Length != neuronAmount[0])
        {
            Debug.LogError($"Input length ({inputs.Length}) does not match first layer neuron count ({neuronAmount[0]})");
            return new float[neuronAmount[layerAmount - 1]]; // Return empty output array
        }

        for (int i = 0; i < neuronAmount[0]; i++)
        {
            neurons[0][i] = inputs[i]; //sets inputs to first layer neurons
        }

        for (int i = 1; i < layerAmount; i++) // Loop through each layer starting from the first hidden layer
        {
            for (int j = 0; j < neurons[i].Length; j++) // Loop through each neuron in the current layer
            {
                float value = 0.25f;
                for (int k = 0; k < neurons[i - 1].Length; k++) // Loop through each neuron in the previous layer
                {
                    value += neurons[i - 1][k] * weights[i - 1][k][j];
                }
                neurons[i][j] = (float)Math.Tanh(value); // Set value of current layer neuron between -1 and 1
            }
        }

        return neurons[layerAmount - 1];
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
        lastPosition = rb.position;
        SetNewExplorationDirection();
    }

    // Update is called once per frame
    void Update()
    {
        if (!foundPlayer)
        {
            // Check if the bot is stuck
            if (Vector2.Distance(rb.position, lastPosition) < minMovementThreshold)
            {
                stuckTime += Time.deltaTime;
                if (stuckTime > stuckThreshold)
                {
                    SetNewExplorationDirection();
                    stuckTime = 0f;
                    jitterPreventionTimer = jitterPreventionDuration; // Prevent jittering for a short duration
                }
            }
            else
            {
                stuckTime = 0f;
            }

            // Update exploration direction periodically
            explorationTimer += Time.deltaTime;
            if (explorationTimer > explorationInterval)
            {
                SetNewExplorationDirection();
                explorationTimer = 0f;
            }

            // Decrease jitter prevention timer
            if (jitterPreventionTimer > 0)
            {
                jitterPreventionTimer -= Time.deltaTime;
            }

            // Get output and normalize it to maintain constant speed
            float[] outputArray = outputs(inputs());
            if (outputArray.Length >= 2)
            {
                Vector2 movement = new Vector2(outputArray[0], outputArray[1]);
                movement.Normalize(); // Ensure movement has a constant magnitude

                // Blend the neural network output with the exploration direction
                movement = Vector2.Lerp(movement, currentExplorationDirection, 0.5f);

                // Apply wall avoidance
                Vector2 avoidanceForce = CalculateWallAvoidance();
                movement += avoidanceForce;

                // Normalize the movement vector again to maintain constant speed
                movement.Normalize();

                // Set the target velocity
                targetVelocity = movement * moveSpeed;

                // Smoothly interpolate current velocity towards target velocity
                rb.velocity = Vector2.SmoothDamp(rb.velocity, targetVelocity, ref currentVelocity, smoothTime);
            }
            else
            {
                Debug.LogError("Output array does not have enough elements");
            }

            lastPosition = rb.position;
        }

        // Debug print for specific bot (e.g., bot with name "11")
        if (name == "11")
        {
            float[] inputArray = inputs();
            float[] outputArray = outputs(inputArray);
            if (inputArray.Length >= 2 && outputArray.Length >= 2)
            {
                print($"ix:{inputArray[0]}, iy:{inputArray[1]}, ox:{outputArray[0]}, oy:{outputArray[1]}");
            }
            else
            {
                Debug.LogError("Input or output array does not have enough elements for debug print");
            }
        }

        // Increase score based on exploration and reduce it based on the distance from the destination
        score += exploredCells.Count * 0.1f * Time.deltaTime;
        score -= (Mathf.Abs(transform.position.y - destinationY) + Mathf.Abs(transform.position.x - destinationX)) * Time.deltaTime * 0.5f;
    }

    private void SetNewExplorationDirection()
    {
        currentExplorationDirection = UnityEngine.Random.insideUnitCircle.normalized;
    }

    private Vector2 CalculateWallAvoidance()
    {
        Vector2 avoidanceForce = Vector2.zero;
        RaycastHit2D[] hits = new RaycastHit2D[8];
        Vector2[] directions = {
            Vector2.up, Vector2.down, Vector2.left, Vector2.right,
            new Vector2(1, 1).normalized, new Vector2(1, -1).normalized,
            new Vector2(-1, 1).normalized, new Vector2(-1, -1).normalized
        };

        for (int i = 0; i < 8; i++)
        {
            hits[i] = Physics2D.Raycast(transform.position, directions[i], wallDetectionDistance);
            if (hits[i].collider != null && hits[i].collider.CompareTag("wall"))
            {
                avoidanceForce -= directions[i] * (wallDetectionDistance - hits[i].distance) / wallDetectionDistance * wallAvoidanceForce;
            }
        }

        return avoidanceForce;
    }

    private void UpdateExploredCells()
    {
        Vector2Int currentCell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x / cellSize),
            Mathf.FloorToInt(transform.position.y / cellSize)
        );
        exploredCells.Add(currentCell);
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.tag == "wall")
        {
            Vector2 wallNormal = col.contacts[0].normal;
            float offset = 0.2f; // Increased offset to move away from walls more quickly

            // Move the bot slightly away from the wall
            rb.MovePosition((Vector2)transform.position + wallNormal * offset);

            // Set a new exploration direction away from the wall
            currentExplorationDirection = wallNormal.normalized;

            score -= 50f * Time.deltaTime; // Increased penalty for staying on walls
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.tag == "edge")
        {
            Vector2 edgeNormal = col.contacts[0].normal;
            float offset = 0.2f; // Increased offset to move away from edges more quickly

            // Move the bot slightly away from the edge
            rb.MovePosition((Vector2)transform.position + edgeNormal * offset);

            // Set a new exploration direction away from the edge
            currentExplorationDirection = edgeNormal.normalized;

            score -= 100f; // Added penalty for hitting edges
        }
        else if (col.gameObject.tag == "plr")
        {
            score += 10000;
            foundPlayer = true;
        }
    }
}
