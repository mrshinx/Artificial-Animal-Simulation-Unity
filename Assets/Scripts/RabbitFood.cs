using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RabbitFood : MonoBehaviour
{
    [Tooltip("The maximum amount of food value remaining of the food object")]
    public float maxFoodAmount;
    [Tooltip("The current amount of food value remaining of the food object")]
    public float foodAmount;
    [Tooltip("The amount of food value regenerated overtime")]
    public float foodReplenish;
    [Tooltip("The maximum amount of water value remaining of the food object")]
    public float maxWaterAmount;
    [Tooltip("The current amount of water value remaining of the food object")]
    public float waterAmount;
    [Tooltip("The amount of water value regenerated overtime")]
    public float waterReplenish;
    [Tooltip("The type of this source of water/food")]
    public SourceType sourceType;
    public enum SourceType { foodSource, waterSource}

    /// <summary>
    /// Whether the food object has any "food value"
    /// </summary>
    public bool HasFood
    {
        get
        {
            return foodAmount > 0;
        }
    }

    /// <summary>
    /// Whether the food object has any "water value"
    /// </summary>
    public bool HasWater
    {
        get
        {
            return waterAmount > 0;
        }
    }

    public float Eat(float feedAmount)
    {
        float amountToFeed = Mathf.Clamp(feedAmount, 0, foodAmount);
        foodAmount -= amountToFeed;
        return amountToFeed;
    }

    public float Drink(float feedAmount)
    {
        float amountToFeed = Mathf.Clamp(feedAmount, 0, waterAmount);
        if (maxFoodAmount != 0) amountToFeed = amountToFeed * (maxWaterAmount / maxFoodAmount);
        waterAmount -= amountToFeed;
        return amountToFeed;
    }

    void Start()
    {
        foodAmount = maxFoodAmount;
        waterAmount = maxWaterAmount;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        foodAmount = Mathf.Clamp(foodAmount + foodReplenish, 0, maxFoodAmount) ;
        waterAmount = Mathf.Clamp(waterAmount + waterReplenish, 0, maxWaterAmount) ;
    }
}
