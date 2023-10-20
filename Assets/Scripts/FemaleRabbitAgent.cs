//using System;
//using System.Collections;
//using System.Collections.Generic;
//using Unity.MLAgents;
//using Unity.MLAgents.Actuators;
//using Unity.MLAgents.Sensors;
//using UnityEngine;

//public class FemaleRabbitAgent : RabbitAgent
//{

//    public bool pregnant = false;

//    public override void ResetStat()
//    {
//        base.ResetStat();
//        pregnant = false;
//    }

//    public override void CollectObservations(VectorSensor sensor)
//    {
//        sensor.AddObservation(hungerMeter / 120f); // float: 1
//        sensor.AddObservation(thirstMeter / 120f); // float: 1
//        sensor.AddObservation(lifePoint / 100f); // float: 1
//        sensor.AddObservation(touching); // bool: 1
//        sensor.AddObservation(pregnant); // bool: 1
//        // sensor.AddObservation(rb.velocity.magnitude/10f); // float: 1
//        sensor.AddObservation(rb.velocity.normalized); // Vector2: 2
//        sensor.AddObservation(transform.localPosition); // Vector3: 3
//    }
//    /// <summary>
//    /// Called when and action is received from either the player input or the neural network
//    /// 
//    /// vectorAction[i] represents:
//    /// Index 0: move vector x (+1 = right, -1 = left)
//    /// Index 1: move vector y (+1 = up, -1 = down)
//    /// Index 2: look vector x (+1 = right, -1 = left)
//    /// Index 3: look vector y (+1 = up, -1 = down)
//    /// </summary>
//    /// <param name="vectorAction">The actions to take</param>
//    public override void OnActionReceived(ActionBuffers actions)
//    {

//        //// Calculate look direction
//        //Vector3 look = new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], 0);
//        //// Calculate rotate angle
//        //float angle = Vector3.SignedAngle(Vector3.up, look, Vector3.forward);
//        //// Rotate vision accordingly
//        //vision.transform.eulerAngles = new Vector3(0, 0, angle);


//        // 0: stay
//        // 1: turn left
//        // 2: turn right
//        switch (actions.DiscreteActions[0])
//        {
//            case 1:
//                lookAngel += 1;
//                break;
//            case 2:
//                lookAngel -= 1;
//                break;
//        }
//        vision.transform.eulerAngles = new Vector3(0, 0, lookAngel);

//        // Rotate sprite according to direction
//        Vector3 lookVector = Quaternion.AngleAxis(vision.transform.eulerAngles.z, Vector3.forward) * Vector3.up;
//        if (lookVector.x < 0)
//        {
//            transform.localScale = new Vector3(-1f, transform.localScale.y, transform.localScale.z);
//        }
//        else transform.localScale = new Vector3(1f, transform.localScale.y, transform.localScale.z);


//        // 0: lie
//        // 1: sit
//        // 2: idle
//        // 3: walk
//        // 4: run
//        switch (actions.DiscreteActions[1])
//        {
//            case 0:
//                lie = true;
//                sit = false;
//                AddReward(0.0004f);
//                rb.velocity = lookVector.normalized * moveSpeed * 0;
//                hungerMeter = Mathf.Clamp(hungerMeter - 1 / 6 * foodConsumedSprinting, 0, 120); // Last 6 times as long compared to sprinting.
//                thirstMeter = Mathf.Clamp(thirstMeter - 1 / 6 * foodConsumedSprinting, 0, 120); // Last 6 times as long compared to sprinting.
//                break;
//            case 1:
//                lie = false;
//                sit = true;
//                AddReward(0.0002f);
//                rb.velocity = lookVector.normalized * moveSpeed * 0;
//                hungerMeter = Mathf.Clamp(hungerMeter - 0.2f * foodConsumedSprinting, 0, 120); // Last 5 times as long compared to sprinting.
//                thirstMeter = Mathf.Clamp(thirstMeter - 0.2f * foodConsumedSprinting, 0, 120); // Last 5 times as long compared to sprinting.
//                break;
//            case 2:
//                lie = false;
//                sit = false;
//                rb.velocity = lookVector.normalized * moveSpeed * 0;
//                hungerMeter = Mathf.Clamp(hungerMeter - 0.25f * foodConsumedSprinting, 0, 120); // Last 4 times as long compared to sprinting.
//                thirstMeter = Mathf.Clamp(thirstMeter - 0.25f * foodConsumedSprinting, 0, 120); // Last 4 times as long compared to sprinting.
//                break;
//            case 3:
//                lie = false;
//                sit = false;
//                rb.velocity = lookVector.normalized * moveSpeed;
//                hungerMeter = Mathf.Clamp(hungerMeter - 0.5f * foodConsumedSprinting, 0, 120); // Last twice as long compared to sprinting.
//                thirstMeter = Mathf.Clamp(thirstMeter - 0.5f * foodConsumedSprinting, 0, 120); // Last twice as long compared to sprinting.
//                break;
//            case 4:
//                lie = false;
//                sit = false;
//                rb.velocity = lookVector.normalized * moveSpeed * 2;
//                hungerMeter = Mathf.Clamp(hungerMeter - foodConsumedSprinting, 0, 120); // Rabbit should be able to constantly sprinting for 15 mins until hunger gets to 0.
//                thirstMeter = Mathf.Clamp(thirstMeter - foodConsumedSprinting, 0, 120); // Rabbit should be able to constantly sprinting for 15 mins until hunger gets to 0.
//                break;
//        }


//        if (hungerMeter > 80f) AddReward(0.00001f);
//        if (hungerMeter < 50f) { AddReward(-0.00025f * Mathf.Pow(10f, (50 - hungerMeter) / 50f)); }

//        if (thirstMeter < 50f) { AddReward(-0.00025f * Mathf.Pow(10f, (50 - thirstMeter) / 50f)); }

//        if (hungerMeter > 0 && thirstMeter > 0) { lifePoint = Mathf.Clamp(lifePoint + 2 * lpLostHunger, 0, 100); } // Takes 1.5 days for rabbit to recover
//        if (hungerMeter <= 0) { lifePoint -= lpLostHunger; } // Takes 3 days for rabbit to die from hunger
//        if (thirstMeter <= 0) { lifePoint -= lpLostThirst; } // Takes 1 day for rabbit to die from thirst

//        if (lifePoint <= 0) { AddReward(-5f); EndEpisode(); }

//    }

//    /// <summary>
//    /// When Behavior Type is set to "Heuristic Only" on the agent's Behavior Parameters,
//    /// this function will be called. Its return values will be fed into
//    /// <see cref="OnActionReceived(float[])"/> instead of using the neural network
//    /// </summary>
//    /// <param name="actionsOut">And output action array</param>
//    public override void Heuristic(in ActionBuffers actionsOut)
//    {
//        // Add the 3 movement values, pitch, and yaw to the actionsOut array
//        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
//        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
//        if (Input.GetKey(KeyCode.A))
//        {
//            Debug.Log("a pressed");
//            discreteActions[0] = 1;
//        }
//        if (Input.GetKey(KeyCode.D))
//        {
//            discreteActions[0] = 2;
//        }
//        discreteActions[1] = 0;
//    }


//    private void OnCollisionExit2D(Collision2D collision)
//    {
//        touching = false;
//    }

//    public override void CollisonEnterOrStay(Collision2D collision)
//    {
//        touching = true;
//        if (collision.gameObject.CompareTag("Wall")) { AddReward(-0.5f); return; }
//        if (collision.gameObject.CompareTag("Water"))
//        {
//            Drink(collision);
//        }
//        if (collision.gameObject.CompareTag("Male Rabbit"))
//        {
//            if (pregnant)
//            {
//                AddReward(-0.03f);
//                return;
//            }

//            AddReward(0.03f);
//        }
//    }

//    public void Fertilize()
//    {
//        if (UnityEngine.Random.value < 0.35) //%35 percent chance
//        {
//            gameObject.GetComponent<SpriteRenderer>().color = new Color(252 / 255f, 255 / 255f, 104 / 255f);

//            pregnant = true;
//            StartCoroutine(PregnancyPeriod());
//        }
//    }

//    IEnumerator PregnancyPeriod()
//    {
//        yield return new WaitForSeconds(24 * 60 * 60 / timeScale); //30 days
//        pregnant = false;
//        gameObject.GetComponent<SpriteRenderer>().color = new Color(255 / 255f, 138 / 255f, 228 / 255f);
//        // trainingArea.SpawnNewRabbits();
//        StopCoroutine(PregnancyPeriod());
//    }
//}
