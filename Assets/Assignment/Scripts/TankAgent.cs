using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
public class TankAgent : Agent 
{
    private const float forceMultiplier = 20f;
    private const float shootRange = 30f;
    private const int targetScore = 20;

    private static int totalEpisodes = 0;
    private static int successfulEpisodes = 0;

    public static int Score { get; private set; } = 0;

    private float episodeStartTime;

    private float runningTotalTime = 0;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void OnEpisodeBegin()
    {
        // Reset agent's x position
        transform.localPosition = new Vector3(0f, 0f, -35f);
        Score = 0; 
        episodeStartTime = Time.time;
        totalEpisodes++;
        Debug.Log("Number of successful episodes" + successfulEpisodes + "Percentage of successful episodes" + (float)successfulEpisodes / totalEpisodes);
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        // State observation includes:

        // 1. Agent's x position
        sensor.AddObservation(transform.localPosition.x);

        // 2. Each unit’s observation of enemy/friendly tank: [relative_x, relative_z, distance, type_indicator]
        // Use 1 to represent enemies and 0 for friendlies.

        // Find all enemy tanks in the scene
        GameObject[] enemyTanks = GameObject.FindGameObjectsWithTag("EnemyAI");
        int enemyCount = 0;
        foreach (GameObject enemyTank in enemyTanks)
        {
            if (enemyCount >= 6) break;
            // Calculate relative position
            Vector3 relativePosition = enemyTank.transform.localPosition - transform.localPosition;

            // Calculate distance
            float distance = relativePosition.magnitude;

            // Add observations for enemy tank
            sensor.AddObservation(relativePosition.x);
            sensor.AddObservation(relativePosition.z);
            sensor.AddObservation(distance);
            sensor.AddObservation(1); // Type indicator for enemy

            enemyCount++;
        }

        // Find all friendly tanks in the scene
        GameObject[] friendlyTanks = GameObject.FindGameObjectsWithTag("Friendly");
        int friendlyCount = 0;
        foreach (GameObject friendlyTank in friendlyTanks)
        {
            if (friendlyCount >= 6) break;
            // Calculate relative position
            Vector3 relativePosition = friendlyTank.transform.localPosition - transform.localPosition;

            // Calculate distance
            float distance = relativePosition.magnitude;

            // Add observations for friendly tank
            sensor.AddObservation(relativePosition.x);
            sensor.AddObservation(relativePosition.z);
            sensor.AddObservation(distance);
            sensor.AddObservation(0); // Type indicator for friendly

            friendlyCount++;
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // The neural network's output is passed down here
        // Receive action data from the neural network and apply it as a force to the agent's rigid body: actions.ContinuousActions[0] is the x-axis force
        // actions.DiscreteActions[0] is if to shoot, if = 1 means shoot, else only move
        float moveX = actions.ContinuousActions[0];
        bool shouldShoot = actions.DiscreteActions[0] == 1;

        // Apply the movement force to the agent's rigid body
        transform.position += new Vector3(moveX, 0, 0) * forceMultiplier * Time.deltaTime; 

        // Penalize the agent if it falls off the plane
        if (transform.position.x < -30f || transform.position.x > 30f)
        {
            AddReward(-6.0f);
            Debug.Log("falloff plane");
            EndEpisode(); // End the episode if the agent falls off
        }
    
        // Attack the enemy tank if it is within range
        if (shouldShoot)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, shootRange))
            {
                if (hit.collider.CompareTag("EnemyAI"))
                {
                    // Give rewards if the agent is able to hit the enemy tank
                    Destroy(hit.collider.gameObject); // Destroy the enemy tank
                    AddReward(2.0f);
                    Score += 2;
                    if (Score >= targetScore)
                    {
                        successfulEpisodes++;
                        Debug.Log("calling EndEpisode()");
                        float timeTaken = Time.time - episodeStartTime;
                        Debug.Log("Target score reached in: " + timeTaken + " seconds");
                        runningTotalTime += timeTaken;
                        Debug.Log("Average time taken: " + runningTotalTime / successfulEpisodes);
                        EndEpisode(); // End the episode when the target score is reached
                    }
                }
                else if (hit.collider.CompareTag("Friendly"))
                {
                    // Penalize the agent if it hits the friendly tank
                    Destroy(hit.collider.gameObject); // Destroy the friendly tank
                    AddReward(-1.0f);
                    Score -= 1;
                }
            }

        }
    
        // Add a small negative reward to encourage efficiency
        AddReward(-0.0005f); 
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("EnemyAI"))
        {
            // End the game if the agent collides with an enemy tank
            AddReward(-3.0f);
            EndEpisode();
        }
        else if (collision.collider.CompareTag("Friendly")){
            // Prevent tank from colliding into friendly. This negative reward is set to encourage friendly to get past the line so can +2
            AddReward(-1.0f);
            Destroy(collision.collider.gameObject); 
        }
    }

    private void Update()
    {
        // Check if any enemy tank has passed the line with the z value of the agent
        GameObject[] enemyTanks = GameObject.FindGameObjectsWithTag("EnemyAI");
        foreach (GameObject enemyTank in enemyTanks)
        {
            if (enemyTank.transform.localPosition.z < transform.localPosition.z)
            {
                // Decrement the score if an enemy tank gets past the line
                Score -= 1;
                AddReward(-1.0f);
                Destroy(enemyTank); 
            }
        }
        // Similarly, check if any friendly tank has passed the line with the z value of the agent
        GameObject[] friendlyTanks = GameObject.FindGameObjectsWithTag("Friendly");
        foreach (GameObject friendlyTank in friendlyTanks)
        {
            if (friendlyTank.transform.localPosition.z < transform.localPosition.z)
            {
                // Increment the score if a friendly tank gets past the line
                Score += 2;
                AddReward(2.0f);
                Destroy(friendlyTank);
            }
        }
    }

}

