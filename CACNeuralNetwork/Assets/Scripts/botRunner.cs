using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;

public class botRunner : MonoBehaviour
{
    public int botAmount;//needs to be even
    public GameObject bot;
    GameObject[] bots;
    float[] scores;
    int botNum;
    float timer = 0;

    void firstGen()
    {
        for (botNum = 1; botNum < bots.Length; botNum++)
        {
            bots[botNum - 1] = Instantiate(bot);
            bots[botNum - 1].name = (botNum).ToString();
            bots[botNum - 1].GetComponent<neuralNetwork>().id = botNum;
            scores[botNum - 1] = bots[botNum - 1].GetComponent<neuralNetwork>().score;
        }
    }
    void nextGen()
    {
        for(int i = 0; i < scores.Length - 1; i++)
        {
            scores[i] = bots[i].GetComponent<neuralNetwork>().score;
        }
        float medianScore = GetMedian(scores);
        // lists are more optimized
        List<float> scoresList = new List<float>();
        List<GameObject> botsList = new List<GameObject>();

        for (int i = 0; i < bots.Length; i++)
        {
            if (scores[i] > medianScore)
            {
                botsList.Add(bots[i]); // use Add to add  bot to  list if it survived
                scoresList.Add(scores[i]); //set score to id for testing
            }
            else
            {
                Destroy(bots[i]);
            }
        }

        scores = new float[botAmount];
        bots = new GameObject[botAmount];

        // set first half of bots array to the bots that survived
        int index = 0;
        for (int i = 0; i < botsList.Count; i++)
        {
            scores[i] = 0;
            botsList[i].transform.position = Vector2.zero;
            bots[i] = botsList[i];
            index++;
        }
        // create children to fill the remaining spots
        for (int i = index; i < botAmount; i++)
        {
            if (i - index < botsList.Count && botsList[i - index] != null) // Make sure there is a parent bot to create a child from
            {
                scores[i] = 0;
                bots[i] = botsList[i - index].GetComponent<neuralNetwork>().createChild(botNum);
                botNum++;
            }
            else
            {
                // If no parent is available, instantiate a new bot
                //bots[i] = Instantiate(bot);
                //bots[i].name = "AHHHHHHHHHHHHH";
                //botNum++;
            }
        }
    }


    static float GetMedian(float[] array)
    {
        //Sort the array
        float[] sortedArray = array.OrderBy(num => num).ToArray();
        int length = sortedArray.Length;
        //Calculate  median
        if (length % 2 == 1) // if odd
        {
            return sortedArray[length / 2];
        }
        else // if even
        {
            float mid1 = sortedArray[(length / 2) - 1];
            float mid2 = sortedArray[length / 2];
            return (mid1 + mid2) / 2f;
        }
    }


    // Start is called before the first frame update
    void Start()

    {
        bots = new GameObject[botAmount];
        scores = new float[botAmount];
        firstGen();
    }

    // Update is called once per frame
    void Update()
    {

        timer += Time.deltaTime;
        if(timer >= 100)
        {
            timer = 0;
            nextGen();
        }
    }
}