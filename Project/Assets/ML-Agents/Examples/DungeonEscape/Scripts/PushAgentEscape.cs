using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.IO;
// for referencing
// smc = Start of my code. emc = end of my code.

/// <summary>
/// adapted from: https://docs.unity3d.com/2020.1/Documentation/Manual/JSONSerialization.html
/// </summary>
[System.Serializable]
public class JSONGameMetrics
{
    public float time;
    public float dragonKilledTime;
    public float agentPickedKeyTime;
    public float agentExitTime;
    
}


public class PushAgentEscape : Agent
{

    public GameObject MyKey; //my key gameobject. will be enabled when key picked up.
    public bool IHaveAKey; //have i picked up a key
    private PushBlockSettings m_PushBlockSettings;
    private Rigidbody m_AgentRb;
    private DungeonEscapeEnvController m_GameController;

    public override void Initialize()
    {
        m_GameController = GetComponentInParent<DungeonEscapeEnvController>();
        m_AgentRb = GetComponent<Rigidbody>();
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
        MyKey.SetActive(false);
        IHaveAKey = false;
    }


    //smc
    //custom time logging object
    JSONGameMetrics jsonObject = new JSONGameMetrics();

    //adjust path accordingly
    string path = "CM3070-project-code/custom-metrics-results/run1.json";

    //save results to JSON file
    private void writeToFile(){
        //current simulation time
        jsonObject.time = Time.time;
        
        //convert JSON object into string, for writing to a file
        string json = JsonUtility.ToJson(jsonObject);

        // append current JSON data to file
        File.AppendAllText(path, json.ToString());
    }
    //emc

    public override void OnEpisodeBegin()
    {
        MyKey.SetActive(false);
        IHaveAKey = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(IHaveAKey);
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];

        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
            case 5:
                dirToGo = transform.right * -0.75f;
                break;
            case 6:
                dirToGo = transform.right * 0.75f;
                break;
        }
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        m_AgentRb.AddForce(dirToGo * m_PushBlockSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Move the agent using the action.
        MoveAgent(actionBuffers.DiscreteActions);
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.transform.CompareTag("lock"))
        {
            if (IHaveAKey)
            {
                MyKey.SetActive(false);
                IHaveAKey = false;
                m_GameController.UnlockDoor();

                //smc
                // set time first agent exits
                jsonObject.agentExitTime = Time.time;

                //save JSON data to file
                writeToFile();
                //emc
            }
        }
        if (col.transform.CompareTag("dragon"))
        {
            m_GameController.KilledByBaddie(this, col);
            MyKey.SetActive(false);
            IHaveAKey = false;

            //smc
            // set time dragon killed for evaluation
            jsonObject.dragonKilledTime = Time.time;
            //emc
        }
        if (col.transform.CompareTag("portal"))
        {
            m_GameController.TouchedHazard(this);
        }
    }

    void OnTriggerEnter(Collider col)
    {
        //if we find a key and it's parent is the main platform we can pick it up
        if (col.transform.CompareTag("key") && col.transform.parent == transform.parent && gameObject.activeInHierarchy)
        {
            print("Picked up key");
            MyKey.SetActive(true);
            IHaveAKey = true;
            col.gameObject.SetActive(false);

            //smc
            // time agent picked up key (after dragon has been killed)
            jsonObject.agentPickedKeyTime = Time.time;      
            //emc     
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 4;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
    }
}
