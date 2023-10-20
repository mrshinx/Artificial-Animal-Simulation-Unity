using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class RabbitAgent : Agent
{

    public float timeScale;
    [Tooltip("Bunny movement speed")]
    public float moveSpeed = 0.05f;
    [Tooltip("Whether this is training mode or gameplay mode")]
    public bool trainingMode;
    [Tooltip("Hunger meter, default value 90")]
    public float hungerMeter = 90f;
    [Tooltip("Max value of hunger meter, default value 120")]
    public float maxHungerMeter = 120f;
    [Tooltip("Thirst meter, default value 100")]
    public float thirstMeter = 90f;
    [Tooltip("Max value of thirst meter, default value 120")]
    public float maxThirstMeter = 120f;
    [Tooltip("Stamina meter, default value 100")]
    public float staminaMeter = 100f;
    [Tooltip("Max value of stamina meter, default value 100")]
    public float maxStaminaMeter = 100f;
    [Tooltip("Life point, default value 100")]
    public float lifePoint = 100f;
    [Tooltip("Max value of life point, default value 100")]
    public float maxLifePoint = 100f;
    [Tooltip("Value of sexual satiety, default value 0")]
    public float sexualSatiety = 75f;
    [Tooltip("Max value of max sexual satiety, default value 100")]
    public float maxSexualSatiety = 100f;
    public enum Gender
    {
        Female,
        Male
    }
    [Tooltip("Gender of this rabbit")]
    public Gender gender = Gender.Male;

    private float foodObtained = 0f;
    private float waterObtained = 0f;
    public float ejaculationThreshold = 0f;

    [Tooltip("Game object that contains vision raycast")]
    public GameObject vision;
    [Tooltip("Training Area")]
    public TrainingArea trainingArea;
    public Rigidbody2D rb;
    public Animator animator;

    public bool interact = false;
    public bool rest = false;
    public bool touching = false;
    public bool female = false;
    public bool pregnant = false;
    public bool ovulation = false;
    public float lookAngel = 0;

    public float foodConsumedSprinting;
    public float staminaConsumedSprinting;
    public float lpLostHunger;
    public float lpLostThirst;
    public float lpLostStamina;
    public float sexualSatietyDecrease = 0;

    Coroutine ovulationCoroutine = null;
    Coroutine pregnancyCoroutine = null;

    public override void Initialize()
    {
        // Randomly pick gender and color accordingly
        gender = (Gender)UnityEngine.Random.Range(0, 2);
        if (gender == Gender.Male)
        {
            gameObject.tag = "Male Rabbit";
            gameObject.GetComponent<SpriteRenderer>().color = new Color(88 / 255f, 155 / 255f, 255 / 255f);
            // 6000/(3^-5*60*60*50)
        }
        else
        {
            female = true;
            gameObject.tag = "Female Rabbit";
            gameObject.GetComponent<SpriteRenderer>().color = new Color(255 / 255f, 138 / 255f, 228 / 255f);
        }

        trainingArea = transform.parent.GetComponent<TrainingArea>();
        timeScale = trainingArea.timeScale;

        moveSpeed *= timeScale;

        foodConsumedSprinting = timeScale * 100 / (20 * 60 * 50); // 20 min
        staminaConsumedSprinting = timeScale * 100 / (5 * 60 * 50); // 5 min

        lpLostHunger = timeScale * 100 / (24 * 60 * 60 * 50); // 72hrs
        lpLostThirst = timeScale * 100 / (8 * 60 * 60 * 50); // 24hr
        lpLostStamina = timeScale * 100 / (30 * 60 * 50); // 30 mins

        sexualSatietyDecrease = timeScale * Mathf.Pow(3, -9 - sexualSatiety / maxSexualSatiety); // Takes 32.8 hours to recover at 100 satiety

        Invoke("LifeSpan", 9*365*24*60*60/60); // 9 years
    }

    private void LifeSpan()
    {
        Destroy(gameObject);
    }

    public override void OnEpisodeBegin()
    {
        ResetStat();
        // trainingArea.ResetCarrotSpawn();
        // trainingArea.ResetRabbitSpawn();
    }

    public virtual void ResetStat()
    {
        StopAllCoroutines();
        // Randomly pick gender and color accordingly
        gender = (Gender)UnityEngine.Random.Range(0, 2);
        if (gender == Gender.Male)
        {
            gameObject.tag = "Male Rabbit";
            gameObject.GetComponent<SpriteRenderer>().color = new Color(88 / 255f, 155 / 255f, 255 / 255f);
            female = false;
            // 6000/(3^-5*60*60*50)
        }
        else
        {
            female = true;
            gameObject.tag = "Female Rabbit";
            gameObject.GetComponent<SpriteRenderer>().color = new Color(255 / 255f, 138 / 255f, 228 / 255f);
        }

        hungerMeter = 90f;
        thirstMeter = 90f;
        staminaMeter = 100f;
        lifePoint = 100f;
        sexualSatiety = 75f;
        pregnant = false;
        ovulation = false;
        transform.localPosition = (Vector3)trainingArea.GetNewSpawnPoint() - trainingArea.transform.localPosition;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(hungerMeter / maxHungerMeter); // float: 1
        sensor.AddObservation(thirstMeter / maxThirstMeter); // float: 1
        sensor.AddObservation(staminaMeter / maxStaminaMeter); // float: 1
        sensor.AddObservation(sexualSatiety / maxSexualSatiety); // float: 1
        sensor.AddObservation(ejaculationThreshold/5f); // float: 1
        sensor.AddObservation(lifePoint / maxLifePoint); // float: 1
        sensor.AddObservation(touching); // bool: 1
        sensor.AddObservation(female); // bool: 1
        sensor.AddObservation(pregnant); // bool: 1
        // sensor.AddObservation(rb.velocity.magnitude/10f); // float: 1
        sensor.AddObservation(transform.InverseTransformDirection(rb.velocity)); // Vector3: 3
    }

    /// <summary>
    /// Called when and action is received from either the player input or the neural network
    /// 
    /// vectorAction[i] represents:
    /// Index 0: move vector x (+1 = right, -1 = left)
    /// Index 1: move vector y (+1 = up, -1 = down)
    /// Index 2: look vector x (+1 = right, -1 = left)
    /// Index 3: look vector y (+1 = up, -1 = down)
    /// </summary>
    /// <param name="vectorAction">The actions to take</param>
    public override void OnActionReceived(ActionBuffers actions)
    {

        //// Calculate look direction
        //Vector3 look = new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], 0);
        //// Calculate rotate angle
        //float angle = Vector3.SignedAngle(Vector3.up, look, Vector3.forward);
        //// Rotate vision accordingly
        //vision.transform.eulerAngles = new Vector3(0, 0, angle);


        // 0: stay
        // 1: turn left
        // 2: turn right
        //switch (actions.DiscreteActions[0])
        //{
        //    case 1:
        //        lookAngel += 1;
        //        break;
        //    case 2:
        //        lookAngel -= 1;
        //        break;
        //}
        //vision.transform.eulerAngles = new Vector3(0, 0, lookAngel);

        //// Rotate sprite according to direction
        //Vector3 lookVector = Quaternion.AngleAxis(vision.transform.eulerAngles.z, Vector3.forward) * Vector3.up;

        // Calculate look direction
        Vector3 lookVector = new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], 0);
        // Calculate rotate angle
        float angle = Vector3.SignedAngle(Vector3.up, lookVector, Vector3.forward);
        // Rotate vision accordingly
        vision.transform.eulerAngles = new Vector3(0, 0, angle);
        if (lookVector.x < 0)
        {
            transform.localScale = new Vector3(-1f, transform.localScale.y, transform.localScale.z);
        }
        else transform.localScale = new Vector3(1f, transform.localScale.y, transform.localScale.z);


        // 0: lie
        // 1: sit
        // 2: idle
        // 3: walk
        // 4: run
        switch (actions.DiscreteActions[0])
        {
            case 0:
                interact = false;
                rest = true;
                rb.velocity = lookVector * moveSpeed * 0;
                if (hungerMeter > 50f && thirstMeter > 50f) AddReward(0.002f);
                hungerMeter = Mathf.Clamp(hungerMeter - 0.125f * foodConsumedSprinting, 0, maxHungerMeter); // Last 8 times as long compared to sprinting.
                thirstMeter = Mathf.Clamp(thirstMeter - 0.25f * foodConsumedSprinting, 0, maxThirstMeter); // Last 8 times as long compared to sprinting.
                staminaMeter = Mathf.Clamp(staminaMeter + staminaConsumedSprinting, 0, maxStaminaMeter); // When idling, restore all stamina after 15 mins.
                break;            
            case 1:
                interact = true;
                rest = false;
                rb.velocity = lookVector * moveSpeed * 0;
                hungerMeter = Mathf.Clamp(hungerMeter - 0.25f * foodConsumedSprinting, 0, maxHungerMeter); // Last 4 times as long compared to sprinting.
                thirstMeter = Mathf.Clamp(thirstMeter - 0.5f * foodConsumedSprinting, 0, maxThirstMeter); // Last 4 times as long compared to sprinting.
                staminaMeter = Mathf.Clamp(staminaMeter + staminaConsumedSprinting, 0, maxStaminaMeter); // When idling, restore all stamina after 15 mins.
                break;
            case 2:
                interact = false;
                rest = false;
                rb.velocity = lookVector * moveSpeed;
                hungerMeter = Mathf.Clamp(hungerMeter - 0.5f * foodConsumedSprinting, 0, maxHungerMeter); // Last twice as long compared to sprinting.
                thirstMeter = Mathf.Clamp(thirstMeter - foodConsumedSprinting, 0, maxThirstMeter); // Last twice as long compared to sprinting.
                staminaMeter = Mathf.Clamp(staminaMeter - staminaConsumedSprinting/6f, 0, maxStaminaMeter); // Rabbit should be able to constantly walk for 30 mins until stamina gets to 0.
                break;
            case 3:
                interact = false;
                rest = false;
                rb.velocity = lookVector * moveSpeed * 2;
                hungerMeter = Mathf.Clamp(hungerMeter - foodConsumedSprinting, 0, maxHungerMeter); // Rabbit should be able to sprint for 20 min until hunger gets to 0.
                thirstMeter = Mathf.Clamp(thirstMeter - 2*foodConsumedSprinting, 0, maxThirstMeter); // Rabbit should be able to sprint for 10 min until thirst gets to 0.
                staminaMeter = Mathf.Clamp(staminaMeter - staminaConsumedSprinting, 0, maxStaminaMeter); // Rabbit should be able to constantly sprint for 5 mins until stamina gets to 0.
                break;
        }

        if (hungerMeter < 50f) { AddReward(-0.00025f * Mathf.Pow(10f, (50 - hungerMeter) / 50f)); }
        if (thirstMeter < 50f) { AddReward(-0.00025f * Mathf.Pow(10f, (50 - thirstMeter) / 50f)); }
        if (staminaMeter < 50f) { AddReward(-0.0001f * Mathf.Pow(10f, (50 - staminaMeter) / 50f)); }

        if (hungerMeter > 0 && thirstMeter > 0 && staminaMeter > 0) { lifePoint = Mathf.Clamp(lifePoint + 2 * lpLostHunger, 0, maxLifePoint); } // Takes 1.5 days for rabbit to recover

        if (hungerMeter <= 0) { lifePoint -= lpLostHunger; } // Takes 3 days for rabbit to die from hunger
        if (thirstMeter <= 0) { lifePoint -= lpLostThirst; } // Takes 1 day for rabbit to die from thirst
        if (staminaMeter <= 0) { lifePoint -= lpLostStamina; } // Takes 30 mins for rabbit to die from over exhausting itself

        if (gender == Gender.Male)
        {
            sexualSatiety = Mathf.Clamp(sexualSatiety - sexualSatietyDecrease, 0, 100);
            //if (sexualSatiety < 30f) AddReward(-0.00001f * Mathf.Pow(10f, (30 - sexualSatiety) / 30f));
        }

        if ((lifePoint < 75f && hungerMeter <= 0) || (lifePoint < 75f && thirstMeter <= 0)) { AddReward(-0.0005f * Mathf.Pow(10f, (75 - lifePoint) / 75f)); }
        if (lifePoint <= 0) EndEpisode();

    }

    /// <summary>
    /// When Behavior Type is set to "Heuristic Only" on the agent's Behavior Parameters,
    /// this function will be called. Its return values will be fed into
    /// <see cref="OnActionReceived(float[])"/> instead of using the neural network
    /// </summary>
    /// <param name="actionsOut">And output action array</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Add the 3 movement values, pitch, and yaw to the actionsOut array
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.A))
        {
            Debug.Log("a pressed");
            discreteActions[0] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActions[0] = 2;
        }
        if (Input.GetKey(KeyCode.W))
        {
            discreteActions[1] = 3;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CollisonEnterOrStay(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CollisonEnterOrStay(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        touching = false;
        ejaculationThreshold = 0;
    }

    public virtual void CollisonEnterOrStay(Collision2D collision)
    {
        touching = true;
        if (collision.gameObject.CompareTag("Wall")) { AddReward(-0.0001f); return; }
        if (collision.gameObject.CompareTag("Water") && interact)
        {
            Drink(collision);
        }
        if (gender == Gender.Male && collision.gameObject.CompareTag("Female Rabbit") && interact)
        {
            if (collision.gameObject.GetComponent<RabbitAgent>().pregnant)
            {
                AddReward(-0.003f);
                return;

            }
            Mate(collision);
        }
        if (gender == Gender.Female && collision.gameObject.CompareTag("Male Rabbit") && interact)
        {
            if (pregnant)
            {
                AddReward(-0.003f);
                return;
            }

            AddReward(0.005f);
        }
    }
    private void Mate(Collision2D collision)
    {
        staminaMeter = Mathf.Clamp(staminaMeter - 0.25f * staminaConsumedSprinting, 0, maxStaminaMeter); // Stamina consumed when mating

        if (sexualSatiety >= maxSexualSatiety) AddReward(-0.003f);
        if (sexualSatiety < maxSexualSatiety) AddReward(0.003f * Mathf.Pow(10f, (maxSexualSatiety - sexualSatiety) / maxSexualSatiety));

        ejaculationThreshold += 0.1f;
        if (ejaculationThreshold >= 5f) // 1 min (or 1 sec due to time 60x faster)
        {
            if (sexualSatiety < maxSexualSatiety) AddReward(2f * Mathf.Pow(10f, (maxSexualSatiety - sexualSatiety) / maxSexualSatiety));
            ejaculationThreshold = 0;
            sexualSatiety += 5f;
            collision.gameObject.GetComponent<RabbitAgent>().Fertilize();
        }
    }

    public void Fertilize()
    {
        if (!ovulation)
        {
            ovulation = true;
            ovulationCoroutine = StartCoroutine(OvulationPeriod());
        }
    }
    IEnumerator OvulationPeriod()
    {
        yield return new WaitForSeconds(2 * 60 * 60 / timeScale); //Should be 13 hours
        StopCoroutine(ovulationCoroutine);

        gameObject.GetComponent<SpriteRenderer>().color = new Color(252 / 255f, 255 / 255f, 104 / 255f);

        pregnant = true;
        pregnancyCoroutine = StartCoroutine(PregnancyPeriod());
    }

    IEnumerator PregnancyPeriod()
    {
        yield return new WaitForSeconds(18 * 60 * 60 / timeScale); //Should be 30 days
        pregnant = false;
        ovulation = false;
        gameObject.GetComponent<SpriteRenderer>().color = new Color(255 / 255f, 138 / 255f, 228 / 255f);
        // trainingArea.SpawnNewRabbits();
        StopCoroutine(pregnancyCoroutine);
    }

    /// <summary>
    /// Called when the agent's collider enters a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        TriggerEnterOrStay(other);

    }
    /// <summary>
    /// Called when the agent's collider stays in a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay2D(Collider2D other)
    {
        TriggerEnterOrStay(other);

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        touching = false;
    }

    /// <summary>
    /// Handles when the agen'ts collider enters or stays in a trigger collider
    /// </summary>
    /// <param name="collider">The trigger collider</param>
    private void TriggerEnterOrStay(Collider2D collider)
    {
        touching = true;
        if (!collider.CompareTag("RabbitFood")) { return; }
        if (!collider.GetComponent<RabbitFood>().HasFood && interact) { trainingArea.DespawnFoodObject(collider.gameObject); return; }

        if (interact) Eat(collider);
    }

    private void Eat(Collider2D collider)
    {
        foodObtained = collider.GetComponent<RabbitFood>().Eat(0.01f * timeScale);
        waterObtained = collider.GetComponent<RabbitFood>().Drink(0.01f * timeScale);
        hungerMeter = Mathf.Clamp(hungerMeter + foodObtained, 0, maxHungerMeter);
        thirstMeter = Mathf.Clamp(thirstMeter + waterObtained, 0, maxThirstMeter);

        if (hungerMeter >= maxHungerMeter) AddReward(-0.0001f); // Prevent overeating
        else if (foodObtained > 0)
        {
            AddReward(0.002f * Mathf.Pow(10f, (maxHungerMeter - hungerMeter) / maxHungerMeter));
        }
    }

    public void Drink(Collision2D collision)
    {
        waterObtained = collision.gameObject.GetComponent<RabbitFood>().Drink(0.05f * timeScale);
        thirstMeter = Mathf.Clamp(thirstMeter + waterObtained, 0, maxThirstMeter);

        if (thirstMeter >= maxThirstMeter) AddReward(-0.0001f); // Prevent overeating
        else if (waterObtained > 0)
        {
            AddReward(0.001f * Mathf.Pow(10f, (maxThirstMeter - thirstMeter) / maxThirstMeter));
        }
    }
    // Update is called once per frame
    void Update()
    {
        animator.SetFloat("Speed", rb.velocity.magnitude);
        animator.SetBool("Rest", rest);
    }

    private void FixedUpdate()
    {
    }

}
